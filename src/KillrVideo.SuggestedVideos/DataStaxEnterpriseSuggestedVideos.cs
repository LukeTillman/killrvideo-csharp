using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.SuggestedVideos.MLT;
using KillrVideo.Utils;
using RestSharp;
using Serilog;

namespace KillrVideo.SuggestedVideos.SuggestionImpl
{
    /// <summary>
    /// Makes video suggestions based on data in Cassandra.
    /// </summary>
    public class DataStaxEnterpriseSuggestedVideos : ISuggestVideos
    {
        private static readonly ILogger Logger = Log.ForContext<DataStaxEnterpriseSuggestedVideos>();

        private readonly ISession _session;
        private readonly TaskCache<string, PreparedStatement> _statementCache;
        private readonly IRestClient _restClient;

        public DataStaxEnterpriseSuggestedVideos(ISession session, TaskCache<string, PreparedStatement> statementCache, IRestClient restClient)
        {
            if (session == null) throw new ArgumentNullException("session");
            if (statementCache == null) throw new ArgumentNullException("statementCache");
            if (restClient == null) throw new ArgumentNullException("restClient");
            _session = session;
            _statementCache = statementCache;
            _restClient = restClient;
        }

        /// <summary>
        /// Gets the first 5 videos related to the specified video.
        /// </summary>
        public async Task<RelatedVideos> GetRelatedVideos(RelatedVideosQuery query)
        {
            // Set the base URL of the REST client to use the first node in the Cassandra cluster
            string nodeIp = _session.Cluster.AllHosts().First().Address.Address.ToString();
            _restClient.BaseUrl = new Uri(string.Format("http://{0}:8983/solr", nodeIp));
            
            //WebRequest mltRequest = WebRequest.Create("http://127.0.2.15:8983/solr/killrvideo.videos/mlt?q=videoid%3Asome-uuid&wt=json&indent=true&qt=mlt&mlt.fl=name&mlt.mindf=1&mlt.mintf=1");
            var request = new RestRequest("killrvideo.videos/mlt");
            request.AddParameter("q", string.Format("videoid:\"{0}\"", query.VideoId));
            request.AddParameter("wt", "json");

            // Paging information
            int start;
            if (query.PagingState == null || int.TryParse(query.PagingState, out start) == false)
                start = 0;

            request.AddParameter("start", start);
            request.AddParameter("rows", query.PageSize);

            //MLT Fields to Consider
            request.AddParameter("mlt.fl", "name,description,tags");

            //MLT Minimum Document Frequency - the frequency at which words will be ignored which do not occur in at least this many docs.
            request.AddParameter("mlt.mindf", 2);

            //MLT Minimum Term Frequency - the frequency below which terms will be ignored in the source doc.
            request.AddParameter("mlt.mintf", 2);

            IRestResponse<MLTQueryResult> response = await _restClient.ExecuteTaskAsync<MLTQueryResult>(request).ConfigureAwait(false);

            // Check for network/timeout errors
            if (response.ResponseStatus != ResponseStatus.Completed)
            {
                Logger.Error(response.ErrorException, "Error while querying Solr video suggestions from {host} for {query}", nodeIp, query);
                return new RelatedVideos { VideoId = query.VideoId, Videos = Enumerable.Empty<VideoPreview>(), PagingState = null };
            }

            // Check for HTTP error codes
            if (response.StatusCode != HttpStatusCode.OK)
            {
                Logger.Error("HTTP status code {code} while querying Solr video suggestions from {host} for {query}", (int) response.StatusCode,
                             nodeIp, query);
                return new RelatedVideos { VideoId = query.VideoId, Videos = Enumerable.Empty<VideoPreview>(), PagingState = null };
            }

            // Success
            int nextPageStartIndex = response.Data.Response.Start + response.Data.Response.Docs.Count;
            string pagingState = nextPageStartIndex == response.Data.Response.NumFound ? null : nextPageStartIndex.ToString();
            return new RelatedVideos { VideoId = query.VideoId, Videos = response.Data.Response.Docs, PagingState = pagingState };
        }

        /// <summary>
        /// Gets the personalized video suggestions for a specific user.
        /// </summary>
        public async Task<Dtos.SuggestedVideos> GetSuggestions(SuggestedVideosQuery query)
        {
            // Return the output of a Spark job that runs periodically in the background to populate the video_recommendations table
            // (see the /data/spark folder in the repo for more information)
            PreparedStatement prepared = await _statementCache.NoContext.GetOrAddAsync(
                "SELECT videoid, authorid, name, added_date, preview_image_location FROM video_recommendations WHERE userid=?");

            IStatement bound = prepared.Bind(query.UserId)
                                       .SetAutoPage(false)
                                       .SetPageSize(query.PageSize);

            // The initial query won't have a paging state, but subsequent calls should if there are more pages
            if (string.IsNullOrEmpty(query.PagingState) == false)
                bound.SetPagingState(Convert.FromBase64String(query.PagingState));

            RowSet rows = await _session.ExecuteAsync(bound).ConfigureAwait(false);

            return new Dtos.SuggestedVideos()
            {
                UserId = query.UserId,
                Videos = rows.Select(MapRowToVideoPreview).ToList(),
                PagingState = rows.PagingState != null && rows.PagingState.Length > 0 ? Convert.ToBase64String(rows.PagingState) : null
            };
        }

        private static VideoPreview MapRowToVideoPreview(Row row)
        {
            return new VideoPreview
            {
                VideoId = row.GetValue<Guid>("videoid"),
                AddedDate = row.GetValue<DateTimeOffset>("added_date"),
                Name = row.GetValue<string>("name"),
                PreviewImageLocation = row.GetValue<string>("preview_image_location"),
                UserId = row.GetValue<Guid>("authorid")
            };
        }
    }
}