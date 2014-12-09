using System;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.Utils;
using KillrVideo.VideoCatalog.Messages.Commands;
using KillrVideo.VideoCatalog.Messages.Events;
using Nimbus;
using Nimbus.Handlers;

namespace KillrVideo.VideoCatalog.Handlers
{
    /// <summary>
    /// Adds a YouTube video to the catalog.
    /// </summary>
    public class SubmitYouTubeVideoHandler : IHandleCommand<SubmitYouTubeVideo>
    {
        private readonly ISession _session;
        private readonly TaskCache<string, PreparedStatement> _statementCache;
        private readonly IBus _bus;

        public SubmitYouTubeVideoHandler(ISession session, TaskCache<string, PreparedStatement> statementCache, IBus bus)
        {
            if (session == null) throw new ArgumentNullException("session");
            if (statementCache == null) throw new ArgumentNullException("statementCache");
            if (bus == null) throw new ArgumentNullException("bus");
            _session = session;
            _statementCache = statementCache;
            _bus = bus;
        }

        public async Task Handle(SubmitYouTubeVideo youTubeVideo)
        {
            // Use a batch to insert the YouTube video into multiple tables
            PreparedStatement[] prepared = await _statementCache.NoContext.GetOrAddAllAsync(
                "INSERT INTO videos (videoid, userid, name, description, location, preview_image_location, tags, added_date, location_type) " +
                "VALUES (?, ?, ?, ?, ?, ?, ?, ?, " + VideoCatalogConstants.YouTubeVideoType + ")",
                "INSERT INTO user_videos (userid, added_date, videoid, name, preview_image_location) VALUES (?, ?, ?, ?, ?)",
                string.Format(
                    "INSERT INTO latest_videos (yyyymmdd, added_date, videoid, userid, name, preview_image_location) VALUES (?, ?, ?, ?, ?, ?) USING TTL {0}",
                    VideoCatalogConstants.LatestVideosTtlSeconds));

            // Calculate date-related info and location/thumbnail for YouTube video
            var addDate = DateTimeOffset.UtcNow;
            string yyyymmdd = addDate.ToString("yyyyMMdd");

            string location = youTubeVideo.YouTubeVideoId;      // TODO: Store URL instead of ID?
            string previewImageLocation = string.Format("//img.youtube.com/vi/{0}/hqdefault.jpg", youTubeVideo.YouTubeVideoId);

            var batch = new BatchStatement();
            batch.Add(prepared[0].Bind(youTubeVideo.VideoId, youTubeVideo.UserId, youTubeVideo.Name, youTubeVideo.Description, location,
                                       previewImageLocation, youTubeVideo.Tags, addDate));
            batch.Add(prepared[1].Bind(youTubeVideo.UserId, addDate, youTubeVideo.VideoId, youTubeVideo.Name, previewImageLocation));
            batch.Add(prepared[2].Bind(yyyymmdd, addDate, youTubeVideo.VideoId, youTubeVideo.UserId, youTubeVideo.Name, previewImageLocation));
            batch.SetTimestamp(addDate);

            // Send the batch to Cassandra
            await _session.ExecuteAsync(batch);

            // Tell the world about the new YouTube video
            await _bus.Publish(new YouTubeVideoAdded
            {
                VideoId = youTubeVideo.VideoId,
                UserId = youTubeVideo.UserId,
                Name = youTubeVideo.Name,
                Description = youTubeVideo.Description,
                Location = location,
                PreviewImageLocation = previewImageLocation,
                Tags = youTubeVideo.Tags,
                Timestamp = batch.Timestamp.Value
            });
        }
    }
}
