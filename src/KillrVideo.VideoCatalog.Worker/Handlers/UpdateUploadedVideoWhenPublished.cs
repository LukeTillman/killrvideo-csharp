using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.Uploads.Messages.Events;
using KillrVideo.Utils;
using KillrVideo.VideoCatalog.Messages.Events;
using Nimbus;
using Nimbus.Handlers;

namespace KillrVideo.VideoCatalog.Worker.Handlers
{
    /// <summary>
    /// Updates an uploaded videos information in the catalog once the video has been published and is ready for viewing.
    /// </summary>
    public class UpdateUploadedVideoWhenPublished : IHandleCompetingEvent<UploadedVideoPublished>
    {
        private readonly ISession _session;
        private readonly TaskCache<string, PreparedStatement> _statementCache;
        private readonly IBus _bus;

        public UpdateUploadedVideoWhenPublished(ISession session, TaskCache<string, PreparedStatement> statementCache, IBus bus)
        {
            if (session == null) throw new ArgumentNullException("session");
            if (statementCache == null) throw new ArgumentNullException("statementCache");
            if (bus == null) throw new ArgumentNullException("bus");
            _session = session;
            _statementCache = statementCache;
            _bus = bus;
        }

        public async Task Handle(UploadedVideoPublished publishedVideo)
        {
            // Find the video
            PreparedStatement prepared = await _statementCache.NoContext.GetOrAddAsync("SELECT * FROM videos WHERE videoid = ?");
            BoundStatement bound = prepared.Bind(publishedVideo.VideoId);
            RowSet rows = await _session.ExecuteAsync(bound).ConfigureAwait(false);
            Row videoRow = rows.SingleOrDefault();
            if (videoRow == null)
                throw new InvalidOperationException(string.Format("Could not find video with id {0}", publishedVideo.VideoId));

            var locationType = videoRow.GetValue<int>("location_type");
            if (locationType != VideoCatalogConstants.UploadedVideoType)
                throw new InvalidOperationException(string.Format("Video {0} is not an uploaded video of type {1} but is type {2}", publishedVideo.VideoId,
                                                                  VideoCatalogConstants.UploadedVideoType, locationType));

            // Get some data from the Row
            var userId = videoRow.GetValue<Guid>("userid");
            var name = videoRow.GetValue<string>("name");
            var description = videoRow.GetValue<string>("description");
            var tags = videoRow.GetValue<IEnumerable<string>>("tags");

            // Update the video locations (and write to denormalized tables) via batch
            PreparedStatement[] writePrepared = await _statementCache.NoContext.GetOrAddAllAsync(
                "UPDATE videos USING TIMESTAMP ? SET location = ?, preview_image_location = ?, added_date = ? WHERE videoid = ?",
                "INSERT INTO user_videos (userid, added_date, videoid, name, preview_image_location) VALUES (?, ?, ?, ?, ?) USING TIMESTAMP ?",
                "INSERT INTO latest_videos (yyyymmdd, added_date, videoid, userid, name, preview_image_location) VALUES (?, ?, ?, ?, ?, ?) USING TIMESTAMP ? AND TTL ?"
            );

            // Calculate date-related data for the video
            DateTimeOffset addDate = DateTimeOffset.UtcNow;
            string yyyymmdd = addDate.ToString("yyyyMMdd");

            var batch = new BatchStatement();
            batch.Add(writePrepared[0].Bind(addDate.ToMicrosecondsSinceEpoch(), publishedVideo.VideoUrl, publishedVideo.ThumbnailUrl, addDate,
                                            publishedVideo.VideoId));
            batch.Add(writePrepared[1].Bind(userId, addDate, publishedVideo.VideoId, name, publishedVideo.ThumbnailUrl,
                                            addDate.ToMicrosecondsSinceEpoch()));
            batch.Add(writePrepared[2].Bind(yyyymmdd, addDate, publishedVideo.VideoId, userId, name, publishedVideo.ThumbnailUrl,
                                            addDate.ToMicrosecondsSinceEpoch(), VideoCatalogConstants.LatestVideosTtlSeconds));

            await _session.ExecuteAsync(batch).ConfigureAwait(false);

            // Tell the world about the uploaded video that was added
            await _bus.Publish(new UploadedVideoAdded
            {
                VideoId = publishedVideo.VideoId,
                UserId = userId,
                Name = name,
                Description = description,
                Tags = tags.ToHashSet(),
                Location = publishedVideo.VideoUrl,
                PreviewImageLocation = publishedVideo.ThumbnailUrl,
                Timestamp = addDate
            }).ConfigureAwait(false);
        }
    }
}