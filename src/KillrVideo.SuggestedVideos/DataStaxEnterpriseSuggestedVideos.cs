using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Dse;
using Dse.Graph;
using Serilog;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using KillrVideo.Cassandra;
using KillrVideo.Host.ServiceDiscovery;
using KillrVideo.Protobuf;
using KillrVideo.Protobuf.Services;
using KillrVideo.SuggestedVideos.MLT;
using RestSharp;

using static KillrVideo.SuggestedVideos.GraphDsl.Kv;
using static KillrVideo.SuggestedVideos.GraphDsl.KillrVideoGraphTraversalSourceExtensions;
using static KillrVideo.SuggestedVideos.GraphDsl.KillrVideoGraphTraversalExtensions;
using static KillrVideo.SuggestedVideos.GraphDsl.Enrichment;

using Gremlin.Net.Process.Traversal;
using static Gremlin.Net.Process.Traversal.P;

namespace KillrVideo.SuggestedVideos
{
    /// <summary>
    /// Makes video suggestions based on data in Cassandra.
    /// </summary>
    [Export(typeof(IGrpcServerService))]
    public class DataStaxEnterpriseSuggestedVideos : SuggestedVideoService.SuggestedVideoServiceBase, IConditionalGrpcServerService
    {
        /// <summary>
        // DseSession holds information to both Graph and Cassandra.
        /// </summary>
        private readonly IDseSession _session;

        private readonly Func<Uri, IRestClient> _createRestClient;

        private readonly SuggestionsOptions _options;

        private readonly PreparedStatementCache _statementCache;

        private readonly IFindServices _serviceDiscovery;

        private Task<Uri> _dseSearchUri;

        public ServiceDescriptor Descriptor => SuggestedVideoService.Descriptor;

        private static readonly ILogger Logger = Log.ForContext<DataStaxEnterpriseSuggestedVideos>();

        public DataStaxEnterpriseSuggestedVideos(IDseSession session, PreparedStatementCache statementCache,
                                                 IFindServices serviceDiscovery, Func<Uri, IRestClient> createRestClient,
                                                 SuggestionsOptions options)
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
            return SuggestedVideoService.BindService(this);
        }

        /// <summary>
        /// Returns true if this service should run given the configuration of the host.
        /// </summary>
        public bool ShouldRun()
        {
            // Use this implementation when DSE Search and Spark are enabled in the host config
            return _options.DseEnabled;
        }

        /// <summary>
        /// Gets the first 5 videos related to the specified video.
        /// </summary>
        public override async Task<GetRelatedVideosResponse> GetRelatedVideos(GetRelatedVideosRequest request, ServerCallContext context)
        {
            // Get REST client for dse-search service
            Uri searchUri = await GetDseSearchUri().ConfigureAwait(false);
            IRestClient restClient = _createRestClient(searchUri);

            // Example request: http://127.0.2.15:8983/solr/killrvideo.videos/mlt?q=videoid%3Asome-uuid&wt=json&indent=true&qt=mlt&mlt.fl=name&mlt.mindf=1&mlt.mintf=1
            var solrRequest = new RestRequest("killrvideo.videos/mlt");
            solrRequest.AddParameter("q", $"videoid:\"{request.VideoId.Value}\"");
            solrRequest.AddParameter("wt", "json");

            // Paging information
            int start;
            if (request.PagingState == null || int.TryParse(request.PagingState, out start) == false)
                start = 0;

            solrRequest.AddParameter("start", start);
            solrRequest.AddParameter("rows", request.PageSize);

            //MLT Fields to Consider
            solrRequest.AddParameter("mlt.fl", "name,description,tags");

            //MLT Minimum Document Frequency - the frequency at which words will be ignored which do not occur in at least this many docs.
            solrRequest.AddParameter("mlt.mindf", 2);

            //MLT Minimum Term Frequency - the frequency below which terms will be ignored in the source doc.
            solrRequest.AddParameter("mlt.mintf", 2);

            IRestResponse<MLTQueryResult> solrResponse = await restClient.ExecuteTaskAsync<MLTQueryResult>(solrRequest).ConfigureAwait(false);

            // Start with an empty response
            var response = new GetRelatedVideosResponse { VideoId = request.VideoId };

            // Check for network/timeout errors
            if (solrResponse.ResponseStatus != ResponseStatus.Completed)
            {
                // TODO: Logger.Error(response.ErrorException, "Error while querying Solr video suggestions from {host} for {query}", nodeIp, query);
                return response;
            }

            // Check for HTTP error codes
            if (solrResponse.StatusCode != HttpStatusCode.OK)
            {
                // TODO: Logger.Error("HTTP status code {code} while querying Solr video suggestions from {host} for {query}", (int) response.StatusCode, nodeIp, query);
                return response;
            }

            // Success
            int nextPageStartIndex = solrResponse.Data.Response.Start + solrResponse.Data.Response.Docs.Count;
            string pagingState = nextPageStartIndex == solrResponse.Data.Response.NumFound ? "" : nextPageStartIndex.ToString();
            response.PagingState = pagingState;
            response.Videos.Add(solrResponse.Data.Response.Docs.Select(doc => new SuggestedVideoPreview
            {
                VideoId = doc.VideoId.ToUuid(),
                AddedDate = doc.AddedDate.ToTimestamp(),
                Name = doc.Name,
                PreviewImageLocation = doc.PreviewImageLocation,
                UserId = doc.UserId.ToUuid()
            }));
            return response;
        }

        /// <summary>
        /// Gets the personalized video suggestions for a specific user.
        /// Based on DSL tutorial provided here, we changed the recommendation from pre-aggration
        /// tables to real-time graph recommendation.
        /// https://academy.datastax.com/content/gremlin-dsls-net-dse-graph
        /// </summary>
        public override async Task<GetSuggestedForUserResponse> GetSuggestedForUser(GetSuggestedForUserRequest request,
                                                                                    ServerCallContext context)
        {
            int numberOfVideosExpected = 5;
            int minimumRating          = 4;
            int numToSample            = 1000;
            int minimumLocalRating     = 5;

            string[] vextexProperties = { KeyVideoId, KeyUserId, KeyName, KeyAddedDate, KeyPreviewImage };

            Logger.Information("Request suggested video(s) for user {user}", request.UserId.Value);

            GraphTraversalSource g = DseGraph.Traversal(_session);

            var traversal = g
              // Locate User by its userId
              // V().HasLabel("user").Has("userId", request.UserId.Value)
              .Users(request.UserId.ToGuid().ToString())
              .As("^currentUser")

              // Find all related watched video as they rated
              .Map<Vertex>(__.Out("rated").Dedup().Fold())
              .As("^watchedVideos")

              // go back to our current user
              .Select<Vertex>("^currentUser")
              // for the video's I rated highly...
              .OutE("rated").Has("rating", Gt(minimumRating)).InV()
              // what other users rated those videos highly? (this is like saying "what users share my taste")
              .InE("rated").Has("rating", Gt(minimumRating))
              // but don't grab too many, or this won't work OLTP, and "by('rating')" favors the higher ratings
              .Sample(numToSample).By("rating").OutV()
              // (except me)
              .Where(Neq("^currentUser"))
              // Now we're working with "similar users". For those users who share my taste, grab N highly rated 
              // videos. Save the rating so we can sum the scores later, and use sack() because it does not require 
              // path information. (as()/select() was slow)
              .Local<List<Vertex>>(
                    __.OutE("rated")
                      .Has("rating", Gt(minimumRating))
                      .Limit(minimumLocalRating))
              .Sack(Operator.Assign)
              .By("rating").InV()

              // excluding the videos I have already watched
              .Not(__.Where(Within("^watchedVideos")))

              // Filter out the video if for some reason there is no uploaded edge to a user
              // I found this could be a case where an "uploaded" edge was not created for a video given we don't guarantee graph data
              .Filter(__.In("uploaded").HasLabel("user"))

              // what are the most popular videos as calculated by the sum of all their ratings
              .Group<string, long>()
              .By().By(__.Sack<object>()
              .Sum<long>())

              // now that we have that big map of [video: score], lets order it
              .Order(Scope.Local).By(Column.Values, Order.Decr)
              .Limit<IDictionary<Vertex, long>>(Scope.Local, numberOfVideosExpected)
              .Select<Vertex>(Column.Keys)
              .Unfold<Vertex>()
              .Project<Vertex>("video", "user")
              .By()
              .By(__.In("uploaded"));

            GraphResultSet result = await _session.ExecuteGraphAsync(traversal);
            foreach (IVertex vertex in result.To<IVertex>()) {
                Logger.Information("Result " + vertex);
            }
            // Enforce Async as async Task expected in the signature (DSL is not)
            //IList<IDictionary<string, object>> suggestedVideos = await Task.Run(() =>
                    // Get a transversal (GraphTraversalSource)
                    //DseGraph.Traversal(_session)
                    // Locate current user by its label and user id in the graph (single vertex)
                    //.Users(request.UserId.Value)
                    // Use Recommendation engine (threshold on ratings) to find movies Vertices
                    //.Recommend(numberOfVideosExpected, minimumRating)
                    // Project result to get required attributes in order to build a SuggestedVideoPreview
                    //.Enrich(true, Keys(vextexProperties), InDegree(), OutDegree()).ToList());
           
            // Building GRPC response from list of results vertices (hopefully 'numberOfVideosExpected')
            var grpcResponse = new GetSuggestedForUserResponse
            {
                UserId = request.UserId,
                PagingState = ""
            };
            //grpcResponse.Videos.Add(suggestedVideos.Select(MapVertexVideoToVideoPreview));
            return grpcResponse;
        }

        /// <summary>
        /// Map a Vertex from Graph Transversal up to GRPC Bean
        /// </summary>
        private static SuggestedVideoPreview MapVertexVideoToVideoPreview(IDictionary<string, object> vertexVideo)
        {
            Logger.Information(" + Result : {videoid} + date {}", vertexVideo[KeyVideoId], vertexVideo[KeyAddedDate]);
            return new SuggestedVideoPreview
            {
                VideoId = new Guid(vertexVideo[KeyVideoId].ToString()).ToUuid(),
                //AddedDate = "".ToTimestamp(),
                Name = vertexVideo[KeyName].ToString(),
                PreviewImageLocation = vertexVideo[KeyPreviewImage].ToString(),
                UserId = new Guid(vertexVideo[KeyUserId].ToString()).ToUuid()
            };
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



        private static SuggestedVideoPreview MapRowToVideoPreview(Row row)
        {
            return new SuggestedVideoPreview
            {
                VideoId = row.GetValue<Guid>("videoid").ToUuid(),
                AddedDate = row.GetValue<DateTimeOffset>("added_date").ToTimestamp(),
                Name = row.GetValue<string>("name"),
                PreviewImageLocation = row.GetValue<string>("preview_image_location"),
                UserId = row.GetValue<Guid>("authorid").ToUuid()
            };
        }
    }
}