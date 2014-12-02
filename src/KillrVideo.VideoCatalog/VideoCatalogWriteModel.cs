using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.Utils;
using KillrVideo.VideoCatalog.Messages.Commands;
using KillrVideo.VideoCatalog.Messages.Events;
using Nimbus;

namespace KillrVideo.VideoCatalog
{
    /// <summary>
    /// Writes video catalog data to Cassandra.
    /// </summary>
    public class VideoCatalogWriteModel : IVideoCatalogWriteModel
    {
        private static readonly int LatestVideosTtlSeconds = Convert.ToInt32(TimeSpan.FromDays(7).TotalSeconds);

        // Video types for writes (should match Enum in ReadModel)
        private const int YouTubeVideoType = 0;
        private const int UploadedVideoType = 1;

        private readonly ISession _session;
        private readonly TaskCache<string, PreparedStatement> _statementCache;
        private readonly IBus _bus;

        public VideoCatalogWriteModel(ISession session, TaskCache<string, PreparedStatement> statementCache, IBus bus)
        {
            if (session == null) throw new ArgumentNullException("session");
            if (statementCache == null) throw new ArgumentNullException("statementCache");
            if (bus == null) throw new ArgumentNullException("bus");
            _session = session;
            _statementCache = statementCache;
            _bus = bus;
        }
        
        /// <summary>
        /// Adds a YouTube video to the catalog.
        /// </summary>
        public async Task AddYouTubeVideo(SubmitYouTubeVideo youTubeVideo)
        {
            // Use a batch to insert the YouTube video into multiple tables
            PreparedStatement[] prepared = await _statementCache.NoContext.GetOrAddAllAsync(
                "INSERT INTO videos (videoid, userid, name, description, location, preview_image_location, tags, added_date, location_type) " +
                "VALUES (?, ?, ?, ?, ?, ?, ?, ?, " + YouTubeVideoType + ")",
                "INSERT INTO user_videos (userid, added_date, videoid, name, preview_image_location) VALUES (?, ?, ?, ?, ?)",
                string.Format(
                    "INSERT INTO latest_videos (yyyymmdd, added_date, videoid, userid, name, preview_image_location) VALUES (?, ?, ?, ?, ?, ?) USING TTL {0}",
                    LatestVideosTtlSeconds));

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

        /// <summary>
        /// Adds an uploaded video to the catalog.
        /// </summary>
        public async Task AddUploadedVideo(SubmitUploadedVideo uploadedVideo)
        {
            // Store the information we have now in Cassandra
            PreparedStatement prepared = await _statementCache.NoContext.GetOrAddAsync(
                "INSERT INTO videos (videoid, userid, name, description, tags, location_type) VALUES (?, ?, ?, ?, ?, " + UploadedVideoType + ")");

            BoundStatement bound = prepared.Bind(uploadedVideo.VideoId, uploadedVideo.UserId, uploadedVideo.Name, uploadedVideo.Description,
                                                 uploadedVideo.Tags);
            bound.SetTimestamp(DateTimeOffset.UtcNow);
            await _session.ExecuteAsync(bound);

            // Tell the world we've accepted an uploaded video (it hasn't officially been added until we get a location for the
            // video playback and thumbnail)
            await _bus.Publish(new UploadedVideoAccepted
            {
                VideoId = uploadedVideo.VideoId,
                UploadUrl = uploadedVideo.UploadUrl,
                Timestamp = bound.Timestamp.Value
            });
        }

        /// <summary>
        /// Updates an uploaded video with the playback location for the video and the thumbnail preview location.
        /// </summary>
        public async Task UpdateUploadedVideoLocations(Guid videoId, string videoLocation, string thumbnailLocation)
        {
            // Find the video
            PreparedStatement prepared = await _statementCache.NoContext.GetOrAddAsync("SELECT * FROM videos WHERE videoid = ?");
            BoundStatement bound = prepared.Bind(videoId);
            RowSet rows = await _session.ExecuteAsync(bound);
            Row videoRow = rows.SingleOrDefault();
            if (videoRow == null)
                throw new InvalidOperationException(string.Format("Could not find video with id {0}", videoId));

            var locationType = videoRow.GetValue<int>("location_type");
            if (locationType != UploadedVideoType)
                throw new InvalidOperationException(string.Format("Video {0} is not an uploaded video of type {1} but is type {2}", videoId,
                                                                  UploadedVideoType, locationType));
            
            // Get some data from the Row
            var userId = videoRow.GetValue<Guid>("userid");
            var name = videoRow.GetValue<string>("name");
            var description = videoRow.GetValue<string>("description");
            var tags = videoRow.GetValue<IEnumerable<string>>("tags");

            // Update the video locations (and write to denormalized tables) via batch
            PreparedStatement[] writePrepared = await _statementCache.NoContext.GetOrAddAllAsync(
                "UPDATE videos SET location = ?, preview_image_location = ?, added_date = ? WHERE videoid = ?",
                "INSERT INTO user_videos (userid, added_date, videoid, name, preview_image_location) VALUES (?, ?, ?, ?, ?)",
                string.Format(
                    "INSERT INTO latest_videos (yyyymmdd, added_date, videoid, userid, name, preview_image_location) VALUES (?, ?, ?, ?, ?, ?) USING TTL {0}",
                    LatestVideosTtlSeconds));

            // Calculate date-related data for the video
            DateTimeOffset addDate = DateTimeOffset.UtcNow;
            string yyyymmdd = addDate.ToString("yyyyMMdd");

            var batch = new BatchStatement();
            batch.Add(writePrepared[0].Bind(videoLocation, thumbnailLocation, addDate, videoId));
            batch.Add(writePrepared[1].Bind(userId, addDate, videoId, name, thumbnailLocation));
            batch.Add(writePrepared[2].Bind(yyyymmdd, addDate, videoId, userId, name, thumbnailLocation));
            batch.SetTimestamp(addDate);

            await _session.ExecuteAsync(batch);

            // Tell the world about the uploaded video that was added
            await _bus.Publish(new UploadedVideoAdded
            {
                VideoId = videoId,
                UserId = userId, 
                Name = name,
                Description = description,
                Tags = tags.ToHashSet(),
                Location = videoLocation,
                PreviewImageLocation = thumbnailLocation,
                Timestamp = batch.Timestamp.Value
            });
        }
    }
}