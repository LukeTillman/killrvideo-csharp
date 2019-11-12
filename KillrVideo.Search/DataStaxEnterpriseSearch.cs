﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Dse;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using KillrVideo.Cassandra;
using KillrVideo.Host.ServiceDiscovery;
using KillrVideo.Protobuf;
using KillrVideo.Protobuf.Services;
using KillrVideo.Search.Dtos;
using Newtonsoft.Json;
using RestSharp;
using Method = RestSharp.Method;

namespace KillrVideo.Search  {

    /// <summary>
    /// Searches videos using DataStax Enterprise search (Solr integration).
    /// </summary>
    [Export(typeof(IGrpcServerService))]
    public class DataStaxEnterpriseSearch : SearchService.SearchServiceBase, IConditionalGrpcServerService {
        
        private readonly IDseSession _session;

        private readonly Func<Uri, IRestClient> _createRestClient;

        private readonly SearchOptions _options;

        private readonly PreparedStatementCache _statementCache;

        private readonly IFindServices _serviceDiscovery;

        private Task<Uri> _dseSearchUri;

        public ServiceDescriptor Descriptor => SearchService.Descriptor;

        public DataStaxEnterpriseSearch(IDseSession session, PreparedStatementCache statementCache,
                                        IFindServices serviceDiscovery, Func<Uri, IRestClient> createRestClient,
                                        SearchOptions options)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (statementCache == null) throw new ArgumentNullException(nameof(statementCache));
            if (serviceDiscovery == null) throw new ArgumentNullException(nameof(serviceDiscovery));
            if (createRestClient == null) throw new ArgumentNullException(nameof(createRestClient));
            if (options == null) throw new ArgumentNullException(nameof(options));
            _session = session;
            _statementCache = statementCache;
            _serviceDiscovery = serviceDiscovery;
            _createRestClient = createRestClient;
            _options = options;
            _dseSearchUri = null;
        }

        /// <summary>
        /// Convert this instance to a ServerServiceDefinition that can be run on a Grpc server.
        /// </summary>
        public ServerServiceDefinition ToServerServiceDefinition()
        {
            return SearchService.BindService(this);
        }

        /// <summary>
        /// Returns true if this service should run given the configuration of the host.
        /// </summary>
        public bool ShouldRun()
        {
            // Use this implementation when DSE Search is enabled in the host config
            return _options.DseEnabled;
        }

        /// <summary>
        /// Gets a page of videos for a search query.
        /// </summary>
        public override async Task<SearchVideosResponse> SearchVideos(SearchVideosRequest request, ServerCallContext context)
        {
            // Do a Solr query against DSE search to find videos using Solr's ExtendedDisMax query parser. Query the
            // name, tags, and description fields in the videos table giving a boost to matches in the name and tags
            // fields as opposed to the description field
            // More info on ExtendedDisMax: http://wiki.apache.org/solr/ExtendedDisMax
            string solrQuery = "{ \"q\": \"{!edismax qf=\\\"name^2 tags^1 description\\\"}" + request.Query + "\" }";
            
            PreparedStatement prepared = await _statementCache.GetOrAddAsync(
                "SELECT videoid, userid, name, preview_image_location, added_date FROM videos WHERE solr_query=?");

            // The driver's built-in paging feature just works with DSE Search Solr paging which is pretty cool
            IStatement bound = prepared.Bind(solrQuery)
                                       .SetConsistencyLevel(ConsistencyLevel.LocalOne)      // Search queries only support One / LocalOne
                                       .SetAutoPage(false)
                                       .SetPageSize(request.PageSize);

            // The initial query won't have a paging state, but subsequent calls should if there are more pages
            if (string.IsNullOrEmpty(request.PagingState) == false)
                bound.SetPagingState(Convert.FromBase64String(request.PagingState));

            RowSet rows = await _session.ExecuteAsync(bound).ConfigureAwait(false);
            
            var response = new SearchVideosResponse
            {
                Query = request.Query,
                PagingState = rows.PagingState != null && rows.PagingState.Length > 0 ? Convert.ToBase64String(rows.PagingState) : ""
            };
            response.Videos.Add(rows.Select(MapRowToVideoPreview));
            return response;
        }

        /// <summary>
        /// Gets a list of query suggestions for providing typeahead support.
        /// </summary>
        public override async Task<GetQuerySuggestionsResponse> GetQuerySuggestions(GetQuerySuggestionsRequest request, ServerCallContext context)
        {
            // Get REST client for dse-search service
            Uri searchUri = await GetDseSearchUri().ConfigureAwait(false);
            IRestClient restClient = _createRestClient(searchUri);
            
            var restRequest = new RestRequest("killrvideo.videos/suggest") { Method = Method.POST };
            restRequest.AddParameter("wt", "json");

            // We'll build on every query, but in a real production app, we'd probably only do this occasionally or use the Solr config to setup
            // build on commit/optimize of the index
            restRequest.AddParameter("suggest.build", "true");
            restRequest.AddParameter("suggest.q", request.Query);
            IRestResponse<SearchResult> restResponse = await restClient.ExecuteTaskAsync<SearchResult>(restRequest).ConfigureAwait(false);

            // Start with an empty response (i.e. no suggestions)
            var response = new GetQuerySuggestionsResponse { Query = request.Query };

            // Check for network/timeout errors
            if (restResponse.ResponseStatus != ResponseStatus.Completed)
            {
                // TODO: Logger.Error(response.ErrorException, "Error while querying Solr search suggestions from {host} for {query}", nodeIp, getSuggestions.Query);
                return response;
            }

            // Check for HTTP error codes
            if (restResponse.StatusCode != HttpStatusCode.OK)
            {
                // TODO: Logger.Error("HTTP status code {code} while querying Solr video suggestions from {host} for {query}", (int)response.StatusCode, nodeIp, getSuggestions.Query);
                return response;
            }

            // Success, add responses from DSE Search to gRPC response
            response.Suggestions.Add(
                restResponse.Data.Suggest.SearchSuggester[request.Query].Suggestions.Select(s => s.Term)
            );

            return response;
        }

        private Task<Uri> GetDseSearchUri()
        {
            // Try to minimize the number of times we lookup the search service by caching and reusing the task
            Task<Uri> uri = _dseSearchUri;
            if (uri != null && uri.IsFaulted == false)
                return uri;

            // It's not the end of the world if we have a race here and multiple threads do lookups
            uri = LookupDseSearch();
            _dseSearchUri = uri;
            return uri;
        }

        private async Task<Uri> LookupDseSearch()
        {
            IEnumerable<string> ipAndPorts = await _serviceDiscovery.LookupServiceAsync("dse-search").ConfigureAwait(false);
            return new Uri($"http://{ipAndPorts.First()}/solr");
        }
        
        private static SearchResultsVideoPreview MapRowToVideoPreview(Row row)
        {
            return new SearchResultsVideoPreview
            {
                VideoId = row.GetValue<Guid>("videoid").ToUuid(),
                AddedDate = row.GetValue<DateTimeOffset>("added_date").ToTimestamp(),
                Name = row.GetValue<string>("name"),
                PreviewImageLocation = row.GetValue<string>("preview_image_location"),
                UserId = row.GetValue<Guid>("userid").ToUuid()
            };
        }
    }
}
