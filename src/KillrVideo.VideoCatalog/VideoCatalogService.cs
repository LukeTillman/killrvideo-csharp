using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.Utils;
using KillrVideo.VideoCatalog.Dtos;
using KillrVideo.VideoCatalog.Messages.Events;
using Nimbus;

namespace KillrVideo.VideoCatalog
{
    /// <summary>
    /// An implementation of the video catalog service that stores catalog data in Cassandra and publishes events on a message bus.
    /// </summary>
    public class VideoCatalogService : IVideoCatalogService
    {
        private const int MaxDaysInPastForLatestVideos = 7;
        
        private readonly ISession _session;
        private readonly TaskCache<string, PreparedStatement> _statementCache;
        private readonly IBus _bus;

        public VideoCatalogService(ISession session, TaskCache<string, PreparedStatement> statementCache, IBus bus)
        {
            if (session == null) throw new ArgumentNullException("session");
            if (statementCache == null) throw new ArgumentNullException("statementCache");
            _session = session;
            _statementCache = statementCache;
            _bus = bus;
        }

        /// <summary>
        /// Submits an uploaded video to the catalog.
        /// </summary>
        public async Task SubmitUploadedVideo(SubmitUploadedVideo uploadedVideo)
        {
            var timestamp = DateTimeOffset.UtcNow;

            // Store the information we have now in Cassandra
            PreparedStatement[] prepared = await _statementCache.NoContext.GetOrAddAllAsync(
                "INSERT INTO videos (videoid, userid, name, description, tags, location_type, added_date) VALUES (?, ?, ?, ?, ?, ?, ?) USING TIMESTAMP ?",
                "INSERT INTO user_videos (userid, added_date, videoid, name) VALUES (?, ?, ?, ?) USING TIMESTAMP ?"
            );

            var batch = new BatchStatement();

            batch.Add(prepared[0].Bind(uploadedVideo.VideoId, uploadedVideo.UserId, uploadedVideo.Name, uploadedVideo.Description,
                                       uploadedVideo.Tags, VideoCatalogConstants.UploadedVideoType, timestamp,
                                       timestamp.ToMicrosecondsSinceEpoch()));
            batch.Add(prepared[1].Bind(uploadedVideo.UserId, timestamp, uploadedVideo.VideoId, uploadedVideo.Name,
                                       timestamp.ToMicrosecondsSinceEpoch()));

            await _session.ExecuteAsync(batch).ConfigureAwait(false);

            // Tell the world we've accepted an uploaded video (it hasn't officially been added until we get a location for the
            // video playback and thumbnail)
            await _bus.Publish(new UploadedVideoAccepted
            {
                VideoId = uploadedVideo.VideoId,
                UploadUrl = uploadedVideo.UploadUrl,
                Timestamp = timestamp
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Submits a YouTube video to the catalog.
        /// </summary>
        public async Task SubmitYouTubeVideo(SubmitYouTubeVideo youTubeVideo)
        {
            // Use a batch to insert the YouTube video into multiple tables
            PreparedStatement[] prepared = await _statementCache.NoContext.GetOrAddAllAsync(
                "INSERT INTO videos (videoid, userid, name, description, location, preview_image_location, tags, added_date, location_type) " +
                "VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?) USING TIMESTAMP ?",
                "INSERT INTO user_videos (userid, added_date, videoid, name, preview_image_location) VALUES (?, ?, ?, ?, ?) USING TIMESTAMP ?",
                "INSERT INTO latest_videos (yyyymmdd, added_date, videoid, userid, name, preview_image_location) VALUES (?, ?, ?, ?, ?, ?) USING TIMESTAMP ? AND TTL ?");

            // Calculate date-related info and location/thumbnail for YouTube video
            var addDate = DateTimeOffset.UtcNow;
            string yyyymmdd = addDate.ToString("yyyyMMdd");

            string location = youTubeVideo.YouTubeVideoId;      // TODO: Store URL instead of ID?
            string previewImageLocation = string.Format("//img.youtube.com/vi/{0}/hqdefault.jpg", youTubeVideo.YouTubeVideoId);

            var batch = new BatchStatement();
            batch.Add(prepared[0].Bind(youTubeVideo.VideoId, youTubeVideo.UserId, youTubeVideo.Name, youTubeVideo.Description, location,
                                       previewImageLocation, youTubeVideo.Tags, addDate, VideoCatalogConstants.YouTubeVideoType,
                                       addDate.ToMicrosecondsSinceEpoch()));
            batch.Add(prepared[1].Bind(youTubeVideo.UserId, addDate, youTubeVideo.VideoId, youTubeVideo.Name, previewImageLocation,
                                       addDate.ToMicrosecondsSinceEpoch()));
            batch.Add(prepared[2].Bind(yyyymmdd, addDate, youTubeVideo.VideoId, youTubeVideo.UserId, youTubeVideo.Name, previewImageLocation,
                                       addDate.ToMicrosecondsSinceEpoch(), VideoCatalogConstants.LatestVideosTtlSeconds));
            
            // Send the batch to Cassandra
            await _session.ExecuteAsync(batch).ConfigureAwait(false);

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
                AddedDate = addDate,
                Timestamp = addDate
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the details of a specific video.
        /// </summary>
        public async Task<VideoDetails> GetVideo(Guid videoId)
        {
            PreparedStatement preparedStatement = await _statementCache.NoContext.GetOrAddAsync("SELECT * FROM videos WHERE videoid = ?");
            RowSet rows = await _session.ExecuteAsync(preparedStatement.Bind(videoId)).ConfigureAwait(false);
            return MapRowToVideoDetails(rows.SingleOrDefault());
        }

        /// <summary>
        /// Gets a limited number of video preview data by video id.
        /// </summary>
        public async Task<IEnumerable<VideoPreview>> GetVideoPreviews(ISet<Guid> videoIds)
        {
            if (videoIds == null || videoIds.Count == 0) return Enumerable.Empty<VideoPreview>();

            // Since we're doing a multi-get here, limit the number of previews to 20 to try and enforce some
            // performance sanity.  If we ever needed to do more than that, we might think about a different
            // data model that doesn't involve a multi-get.
            if (videoIds.Count > 20) throw new ArgumentOutOfRangeException("videoIds", "videoIds cannot contain more than 20 video id keys.");

            // As an example, let's do the multi-get using multiple statements at the driver level.  For an example of doing this at
            // the CQL level with an IN() clause, see UserReadModel.GetUserProfiles
            PreparedStatement prepared = await _statementCache.NoContext.GetOrAddAsync(
                "SELECT videoid, userid, added_date, name, preview_image_location FROM videos WHERE videoid = ?");

            // Bind multiple times to the prepared statement with each video id and execute all the gets in parallel, then await
            // the completion of all the gets
            RowSet[] rowSets = await Task.WhenAll(videoIds.Select(id => _session.ExecuteAsync(prepared.Bind(id)))).ConfigureAwait(false);

            // Flatten the rows in the rowsets to VideoPreview objects
            return rowSets.SelectMany(rowSet => rowSet, (_, row) => MapRowToVideoPreview(row));
        }

        /// <summary>
        /// Gets the latest videos added to the site.
        /// </summary>
        public async Task<LatestVideos> GetLastestVideos(GetLatestVideos getVideos)
        {
            // We may need multiple queries to fill the quota
            var results = new List<VideoPreview>();

            // Generate a list of all the possibly bucket dates by truncating now to the day, then subtracting days from that day
            // going back as many days as we're allowed to query back
            DateTimeOffset nowToTheDay = DateTimeOffset.UtcNow.Truncate(TimeSpan.TicksPerDay);
            var bucketDates = Enumerable.Range(0, MaxDaysInPastForLatestVideos + 1)
                                        .Select(day => nowToTheDay.Subtract(TimeSpan.FromDays(day)));

            // If we're going to include paging parameters for the first query, filter out any bucket dates that are more recent
            // than the first video on the page
            bool pageFirstQuery = getVideos.FirstVideoOnPageDate.HasValue && getVideos.FirstVideoOnPageVideoId.HasValue;
            if (pageFirstQuery)
            {
                DateTimeOffset maxBucketToTheDay = getVideos.FirstVideoOnPageDate.Value.Truncate(TimeSpan.TicksPerDay);
                bucketDates = bucketDates.Where(bucketToTheDay => bucketToTheDay <= maxBucketToTheDay);
            }

            DateTimeOffset[] bucketDatesArray = bucketDates.ToArray();

            // TODO: Run queries in parallel instead of sequentially?
            for (var i = 0; i < bucketDatesArray.Length; i++)
            {
                int recordsStillNeeded = getVideos.PageSize - results.Count;

                string bucket = bucketDatesArray[i].ToString("yyyyMMdd");

                // If we're processing a paged request, use the appropriate statement
                PreparedStatement preparedStatement;
                IStatement boundStatement;
                if (pageFirstQuery && i == 0)
                {
                    preparedStatement = await _statementCache.NoContext.GetOrAddAsync(
                        "SELECT * FROM latest_videos WHERE yyyymmdd = ? AND (added_date, videoid) <= (?, ?) LIMIT ?");
                    boundStatement = preparedStatement.Bind(bucket, getVideos.FirstVideoOnPageDate.Value, getVideos.FirstVideoOnPageVideoId.Value,
                                                            recordsStillNeeded);
                }
                else
                {
                    preparedStatement = await _statementCache.NoContext.GetOrAddAsync("SELECT * FROM latest_videos WHERE yyyymmdd = ? LIMIT ?");
                    boundStatement = preparedStatement.Bind(bucket, recordsStillNeeded);
                }

                RowSet rows = await _session.ExecuteAsync(boundStatement).ConfigureAwait(false);

                results.AddRange(rows.Select(MapRowToVideoPreview));

                // If we've got all the records we need, we can quit querying
                if (results.Count >= getVideos.PageSize)
                    break;
            }

            return new LatestVideos
            {
                Videos = results
            };
        }

        /// <summary>
        /// Gets a page of videos for a particular user.
        /// </summary>
        public async Task<UserVideos> GetUserVideos(GetUserVideos getVideos)
        {
            // Figure out if we're getting first page or subsequent page
            PreparedStatement preparedStatement;
            IStatement boundStatement;
            if (getVideos.FirstVideoOnPageAddedDate.HasValue && getVideos.FirstVideoOnPageVideoId.HasValue)
            {
                preparedStatement = await _statementCache.NoContext.GetOrAddAsync(
                    "SELECT * FROM user_videos WHERE userid = ? AND (added_date, videoid) <= (?, ?) LIMIT ?");
                boundStatement = preparedStatement.Bind(getVideos.UserId, getVideos.FirstVideoOnPageAddedDate.Value,
                                                        getVideos.FirstVideoOnPageVideoId.Value, getVideos.PageSize);
            }
            else
            {
                preparedStatement = await _statementCache.NoContext.GetOrAddAsync("SELECT * FROM user_videos WHERE userid = ? LIMIT ?");
                boundStatement = preparedStatement.Bind(getVideos.UserId, getVideos.PageSize);
            }

            RowSet rows = await _session.ExecuteAsync(boundStatement).ConfigureAwait(false);
            return new UserVideos
            {
                UserId = getVideos.UserId,
                Videos = rows.Select(MapRowToVideoPreview).ToList()
            };
        }
        
        /// <summary>
        /// Maps a row to a VideoDetails object.
        /// </summary>
        private static VideoDetails MapRowToVideoDetails(Row row)
        {
            if (row == null) return null;

            var tags = row.GetValue<IEnumerable<string>>("tags");
            return new VideoDetails
            {
                VideoId = row.GetValue<Guid>("videoid"),
                UserId = row.GetValue<Guid>("userid"),
                Name = row.GetValue<string>("name"),
                Description = row.GetValue<string>("description"),
                Location = row.GetValue<string>("location"),
                LocationType = (VideoLocationType) row.GetValue<int>("location_type"),
                Tags = tags == null ? new HashSet<string>() : new HashSet<string>(tags),
                AddedDate = row.GetValue<DateTimeOffset>("added_date")
            };
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