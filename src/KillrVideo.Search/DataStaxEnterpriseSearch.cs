using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Cassandra;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using KillrVideo.Cassandra;
using KillrVideo.Host.Config;
using KillrVideo.Protobuf;
using KillrVideo.Protobuf.Services;
using KillrVideo.Search.Dtos;
using Newtonsoft.Json;
using RestSharp;
using Method = RestSharp.Method;

namespace KillrVideo.Search
{
    /// <summary>
    /// Searches videos using DataStax Enterprise search (Solr integration).
    /// </summary>
    [Export(typeof(IGrpcServerService))]
    public class DataStaxEnterpriseSearch : SearchService.SearchServiceBase, IConditionalGrpcServerService
    {
        private readonly ISession _session;
        private readonly IRestClient _restClient;
        private readonly PreparedStatementCache _statementCache;

        public DataStaxEnterpriseSearch(ISession session, PreparedStatementCache statementCache, IRestClient restClient)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (statementCache == null) throw new ArgumentNullException(nameof(statementCache));
            if (restClient == null) throw new ArgumentNullException(nameof(restClient));
            _session = session;
            _statementCache = statementCache;
            _restClient = restClient;
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
        public bool ShouldRun(IHostConfiguration hostConfig)
        {
            // Use this implementation when DSE Search is enabled in the host config
            return SearchConfig.UseDseSearch(hostConfig);
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

            // Set the base URL of the REST client to use the first node in the Cassandra cluster
             string nodeIp = _session.Cluster.AllHosts().First().Address.Address.ToString();
            _restClient.BaseUrl = new Uri($"http://{nodeIp}:8983/solr");

            var restRequest = new RestRequest("killrvideo.videos/suggest") { Method = Method.POST };
            restRequest.AddParameter("wt", "json");

            // Requires a build after new names are added, added on a safe side.
            restRequest.AddParameter("spellcheck.build", "true");
            restRequest.AddParameter("spellcheck.q", request.Query);
            IRestResponse<SearchSuggestionResult> restResponse = await _restClient.ExecuteTaskAsync<SearchSuggestionResult>(restRequest).ConfigureAwait(false);
            
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

            // Success

            /* 
              The spellcheck.suggestions object that comes back in the JSON looks something like this (for example, searching for 'cat'):

                "suggestions": [
                  "cat",
                  {
                    "numFound": 1,
                    "startOffset": 0,
                    "endOffset": 3,
                    "suggestion": [
                      "cat summer video teaser"
                    ]
                  }
                ]
              
              Yes, that's an array of mixed objects (seriously, WTF kind of an API is that Solr?!). Since the array is mixed, we deserialized
              it as a List<string> where the second element will be the JSON string with the actual data we care about. We need to now run
              deserialization again on that to get at the actual data.
            */
            if (restResponse.Data.Spellcheck.Suggestions.Count >= 2)
            {
                // Deserialize the embedded object
                var suggestions = JsonConvert.DeserializeObject<SearchSpellcheckSuggestions>(restResponse.Data.Spellcheck.Suggestions.Last());

                // Add to response if the object deserialized correctly
                if (suggestions.Suggestion != null)
                {
                    response.Suggestions.Add(suggestions.Suggestion);
                }
            }

            return response;
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