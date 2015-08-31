using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.SuggestedVideos.Dtos;
using KillrVideo.Utils;
using RestSharp;
using Newtonsoft.Json;
using KillrVideo.SuggestedVideos.MLT;

namespace KillrVideo.SuggestedVideos.SuggestionImpl
{
    /// <summary>
    /// Makes video suggestions based on data in Cassandra.
    /// </summary>
    public class SuggestVideos : ISuggestVideos
    {
        private const int RelatedVideosToReturn = 4;

        private readonly ISession _session;
        private readonly TaskCache<string, PreparedStatement> _statementCache;

        public SuggestVideos(ISession session, TaskCache<string, PreparedStatement> statementCache)
        {
            if (session == null) throw new ArgumentNullException("session");
            if (statementCache == null) throw new ArgumentNullException("statementCache");
            _session = session;
            _statementCache = statementCache;
        }

        /// <summary>
        /// Gets the first 5 videos related to the specified video.
        /// </summary>
        public async Task<RelatedVideos> GetRelatedVideos(Guid videoId)
        {
            // Lookup the tags for the video
            PreparedStatement nameforVideoPrepared = await _statementCache.NoContext.GetOrAddAsync("SELECT * FROM videos WHERE videoid = ?");
            BoundStatement nameForVideoBound = nameforVideoPrepared.Bind(videoId);
            RowSet nameRows = await _session.ExecuteAsync(nameForVideoBound).ConfigureAwait(false);
            Row nameRow = nameRows.FirstOrDefault();
            if (nameRow == null)
                return new RelatedVideos { VideoId = videoId, Videos = Enumerable.Empty<VideoPreview>() };
            VideoPreview preview = MapRowToVideoPreview(nameRow);


            var node_ip = _session.Cluster.AllHosts().ElementAt(0).Address.Address.ToString();
            //WebRequest mltRequest = WebRequest.Create("http://127.0.2.15:8983/solr/killrvideo.videos/select?q=name%3ALast&wt=json&indent=true&qt=mlt&mlt.fl=name&mlt.mindf=1&mlt.mintf=1");

            var client = new RestClient("http://" + node_ip + ":8983/solr");
            var request = new RestRequest("killrvideo.videos/select");
            request.AddParameter("q", "name:\"" + preview.Name + "\"");
            request.AddParameter("wt", "json");
            request.AddParameter("qt", "mlt");
            //MLT Fields to Consider
            request.AddParameter("mlt.fl", "name");
            //MLT Minimum Document Frequency - the frequency at which words will be ignored which do not occur in at least this many docs.
            request.AddParameter("mlt.mindf", 1);
            //MLT Minimum Term Frequency - the frequency below which terms will be ignored in the source doc.
            request.AddParameter("mlt.mintf", 1);

            var response = client.Execute(request);
            var content = response.Content;
            var mltQuery = JsonConvert.DeserializeObject<MLTQuery>(content);
            if (mltQuery.responseHeader.status != 0)
                return new RelatedVideos
                {
                    VideoId = videoId,
                    Videos = mltQuery.response.docs
                };
            else
                return new RelatedVideos { VideoId = videoId, Videos = Enumerable.Empty<VideoPreview>() };

        }

        /// <summary>
        /// Maps a row to a VideoPreview object.
        /// </summary>
        private static VideoPreview MapRowToVideoPreview(Row row)
        {
            return new VideoPreview
            {
                VideoId = row.GetValue<Guid>("videoid"),
                AddedDate = row.GetValue<DateTimeOffset>("added_date"),
                Name = row.GetValue<string>("name"),
                PreviewImageLocation = row.GetValue<string>("preview_image_location"),
                UserId = row.GetValue<Guid>("userid")
            };
        }
    }
}