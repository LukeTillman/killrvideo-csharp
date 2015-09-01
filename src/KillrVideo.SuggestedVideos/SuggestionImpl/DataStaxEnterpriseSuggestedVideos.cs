using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.SuggestedVideos.Dtos;
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
        private readonly IRestClient _restClient;

        public DataStaxEnterpriseSuggestedVideos(ISession session, TaskCache<string, PreparedStatement> statementCache, IRestClient restClient)
        {
            if (session == null) throw new ArgumentNullException("session");
            if (statementCache == null) throw new ArgumentNullException("statementCache");
            _session = session;
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
    }
}