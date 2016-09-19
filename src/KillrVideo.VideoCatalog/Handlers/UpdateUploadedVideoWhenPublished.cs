using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cassandra;
using DryIocAttributes;
using Google.Protobuf.WellKnownTypes;
using KillrVideo.Cassandra;
using KillrVideo.MessageBus;
using KillrVideo.Protobuf;
using KillrVideo.Uploads.Events;
using KillrVideo.VideoCatalog.Events;

namespace KillrVideo.VideoCatalog.Handlers
{
    /// <summary>
    /// Updates an uploaded videos information in the catalog once the video has been published and is ready for viewing.
    /// </summary>
    [ExportMany, Reuse(ReuseType.Transient)]
    public class UpdateUploadedVideoWhenPublished : IHandleMessage<UploadedVideoPublished>
    {
        private readonly ISession _session;
        private readonly IBus _bus;
        private readonly PreparedStatementCache _statementCache;

        public UpdateUploadedVideoWhenPublished(ISession session, PreparedStatementCache statementCache, IBus bus)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (statementCache == null) throw new ArgumentNullException(nameof(statementCache));
            if (bus == null) throw new ArgumentNullException(nameof(bus));
            _session = session;
            _statementCache = statementCache;
            _bus = bus;
        }

        public async Task Handle(UploadedVideoPublished publishedVideo)
        {
            // Find the video
            PreparedStatement prepared = await _statementCache.GetOrAddAsync("SELECT * FROM videos WHERE videoid = ?");
            BoundStatement bound = prepared.Bind(publishedVideo.VideoId);
            RowSet rows = await _session.ExecuteAsync(bound).ConfigureAwait(false);
            Row videoRow = rows.SingleOrDefault();
            if (videoRow == null)
                throw new InvalidOperationException($"Could not find video with id {publishedVideo.VideoId}");

            var locationType = videoRow.GetValue<int>("location_type");
            if (locationType != (int) VideoLocationType.Upload)
                throw new InvalidOperationException($"Video {publishedVideo.VideoId} is not an uploaded video of type {VideoLocationType.Upload} but is type {locationType}");

            // Get some data from the Row
            var userId = videoRow.GetValue<Guid>("userid");
            var name = videoRow.GetValue<string>("name");
            var description = videoRow.GetValue<string>("description");
            var tags = videoRow.GetValue<IEnumerable<string>>("tags");
            var addDate = videoRow.GetValue<DateTimeOffset>("added_date");

            // Get some data from the event
            string videoUrl = publishedVideo.VideoUrl;
            string thumbnailUrl = publishedVideo.ThumbnailUrl;
            Guid videoId = publishedVideo.VideoId.ToGuid();
            
            // Update the video locations (and write to denormalized tables) via batch
            PreparedStatement[] writePrepared = await _statementCache.GetOrAddAllAsync(
                "UPDATE videos SET location = ?, preview_image_location = ? WHERE videoid = ?",
                "UPDATE user_videos SET preview_image_location = ? WHERE userid = ? AND added_date = ? AND videoid = ?",
                "INSERT INTO latest_videos (yyyymmdd, added_date, videoid, userid, name, preview_image_location) VALUES (?, ?, ?, ?, ?, ?) USING TTL ?"
            );

            // Calculate date-related data for the video
            string yyyymmdd = addDate.ToString("yyyyMMdd");
            DateTimeOffset timestamp = publishedVideo.Timestamp.ToDateTimeOffset();

            var batch = new BatchStatement();
            batch.Add(writePrepared[0].Bind(videoUrl, thumbnailUrl, videoId))
                 .Add(writePrepared[1].Bind(thumbnailUrl, userId, addDate, videoId))
                 .Add(writePrepared[2].Bind(yyyymmdd, addDate, videoId, userId, name, thumbnailUrl, VideoCatalogServiceImpl.LatestVideosTtlSeconds))
                 .SetTimestamp(timestamp);

            await _session.ExecuteAsync(batch).ConfigureAwait(false);

            // Tell the world about the uploaded video that was added
            var addedEvent = new UploadedVideoAdded
            {
                VideoId = publishedVideo.VideoId,
                UserId = userId.ToUuid(),
                Name = name,
                Description = description,
                Location = publishedVideo.VideoUrl,
                PreviewImageLocation = publishedVideo.ThumbnailUrl,
                AddedDate = addDate.ToTimestamp(),
                Timestamp = timestamp.ToTimestamp()
            };
            addedEvent.Tags.Add(tags);
            await _bus.Publish(addedEvent).ConfigureAwait(false);
        }
    }
}