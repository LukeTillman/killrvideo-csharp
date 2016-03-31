using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Cassandra;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using KillrVideo.Cassandra;
using KillrVideo.Protobuf;
using KillrVideo.SuggestedVideos.MLT;
using RestSharp;

namespace KillrVideo.SuggestedVideos
{
    /// <summary>
    /// Makes video suggestions based on data in Cassandra.
    /// </summary>
    [Export(typeof(IGrpcServerService))]
    public class DataStaxEnterpriseSuggestedVideos : SuggestedVideoService.ISuggestedVideoService, IGrpcServerService
    {
        private readonly ISession _session;
        private readonly IRestClient _restClient;
        private readonly PreparedStatementCache _statementCache;

        public DataStaxEnterpriseSuggestedVideos(ISession session, PreparedStatementCache statementCache, IRestClient restClient)
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
            return SuggestedVideoService.BindService(this);
        }

        /// <summary>
        /// Gets the first 5 videos related to the specified video.
        /// </summary>
        public async Task<GetRelatedVideosResponse> GetRelatedVideos(GetRelatedVideosRequest request, ServerCallContext context)
        {
            // Set the base URL of the REST client to use the first node in the Cassandra cluster
            string nodeIp = _session.Cluster.AllHosts().First().Address.Address.ToString();
            _restClient.BaseUrl = new Uri($"http://{nodeIp}:8983/solr");
            
            //WebRequest mltRequest = WebRequest.Create("http://127.0.2.15:8983/solr/killrvideo.videos/mlt?q=videoid%3Asome-uuid&wt=json&indent=true&qt=mlt&mlt.fl=name&mlt.mindf=1&mlt.mintf=1");
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

            IRestResponse<MLTQueryResult> solrResponse = await _restClient.ExecuteTaskAsync<MLTQueryResult>(solrRequest).ConfigureAwait(false);

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
        /// </summary>
        public async Task<GetSuggestedForUserResponse> GetSuggestedForUser(GetSuggestedForUserRequest request, ServerCallContext context)
        {
            // Return the output of a Spark job that runs periodically in the background to populate the video_recommendations table
            // (see the /data/spark folder in the repo for more information)
            PreparedStatement prepared = await _statementCache.GetOrAddAsync(
                "SELECT videoid, authorid, name, added_date, preview_image_location FROM video_recommendations WHERE userid=?");

            IStatement bound = prepared.Bind(request.UserId.ToGuid())
                                       .SetAutoPage(false)
                                       .SetPageSize(request.PageSize);

            // The initial query won't have a paging state, but subsequent calls should if there are more pages
            if (string.IsNullOrEmpty(request.PagingState) == false)
                bound.SetPagingState(Convert.FromBase64String(request.PagingState));

            RowSet rows = await _session.ExecuteAsync(bound).ConfigureAwait(false);

            var response = new GetSuggestedForUserResponse
            {
                UserId = request.UserId,
                PagingState = rows.PagingState != null && rows.PagingState.Length > 0 ? Convert.ToBase64String(rows.PagingState) : ""
            };
            response.Videos.Add(rows.Select(MapRowToVideoPreview));
            return response;
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