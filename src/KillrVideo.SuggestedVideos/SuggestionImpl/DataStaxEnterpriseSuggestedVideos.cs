using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.SuggestedVideos.Dtos;
using KillrVideo.SuggestedVideos.MLT;
using KillrVideo.Utils;
using Newtonsoft.Json;
using RestSharp;

namespace KillrVideo.SuggestedVideos.SuggestionImpl
{
    /// <summary>
    /// Makes video suggestions based on data in Cassandra.
    /// </summary>
    public class DataStaxEnterpriseSuggestedVideos : ISuggestVideos
    {
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
        public async Task<RelatedVideos> GetRelatedVideos(Guid videoId)
        {
            // Set the base URL of the REST client to use the first node in the Cassandra cluster
            string nodeIp = _session.Cluster.AllHosts().First().Address.Address.ToString();
            _restClient.BaseUrl = new Uri(string.Format("http://{0}:8983/solr", nodeIp));
            
            //WebRequest mltRequest = WebRequest.Create("http://127.0.2.15:8983/solr/killrvideo.videos/mlt?q=videoid%3Asome-uuid&wt=json&indent=true&qt=mlt&mlt.fl=name&mlt.mindf=1&mlt.mintf=1");
            var request = new RestRequest("killrvideo.videos/mlt");
            request.AddParameter("q", string.Format("videoid:\"{0}\"", videoId));
            request.AddParameter("wt", "json");

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
                // TODO: Logging
                return new RelatedVideos { VideoId = videoId, Videos = Enumerable.Empty<VideoPreview>() };
            }

            // Check for HTTP error codes
            if (response.StatusCode != HttpStatusCode.OK)
            {
                // TODO: Logging
                return new RelatedVideos { VideoId = videoId, Videos = Enumerable.Empty<VideoPreview>() };
            }

            // Success
            return new RelatedVideos { VideoId = videoId, Videos = response.Data.Response.Docs };
        }
    }
}