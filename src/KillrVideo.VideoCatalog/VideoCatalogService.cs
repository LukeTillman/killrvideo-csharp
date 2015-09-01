using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
        private static readonly Regex ParseLatestPagingState = new Regex("([0-9]{8}){8}([0-9]{1})(.*)", RegexOptions.Compiled | RegexOptions.Singleline);
        
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
            string[] buckets;
            int bucketIndex;
            string rowPagingState;

            // See if we have paging state from a previous page of videos
            if (TryParsePagingState(getVideos.PagingState, out buckets, out bucketIndex, out rowPagingState) == false)
            {
                // Generate a list of all the possibly bucket dates by truncating now to the day, then subtracting days from that day
                // going back as many days as we're allowed to query back
                DateTimeOffset nowToTheDay = DateTimeOffset.UtcNow.Truncate(TimeSpan.TicksPerDay);
                buckets = Enumerable.Range(0, MaxDaysInPastForLatestVideos + 1)
                                    .Select(day => nowToTheDay.Subtract(TimeSpan.FromDays(day)).ToString("yyyyMMdd"))
                                    .ToArray();
                bucketIndex = 0;
                rowPagingState = null;
            }
            
            // We may need multiple queries to fill the quota so build up a list of results
            var results = new List<VideoPreview>();
            string nextPageState = null;

            // TODO: Run queries in parallel?
            while (bucketIndex < buckets.Length)
            {
                int recordsStillNeeded = getVideos.PageSize - results.Count;
                string bucket = buckets[bucketIndex];

                // Get a page of records but don't automatically load more pages when enumerating the RowSet
                PreparedStatement preparedStatement = await _statementCache.NoContext.GetOrAddAsync("SELECT * FROM latest_videos WHERE yyyymmdd = ?");
                IStatement boundStatement = preparedStatement.Bind(bucket)
                                                             .SetAutoPage(false)
                                                             .SetPageSize(recordsStillNeeded);

                // Start from where we left off in this bucket
                if (string.IsNullOrEmpty(rowPagingState) == false)
                    boundStatement.SetPagingState(Convert.FromBase64String(rowPagingState));

                RowSet rows = await _session.ExecuteAsync(boundStatement).ConfigureAwait(false);
                results.AddRange(rows.Select(MapRowToVideoPreview));

                // See if we can stop querying
                if (results.Count == getVideos.PageSize)
                {
                    // Are there more rows in the current bucket?
                    if (rows.PagingState != null && rows.PagingState.Length > 0)
                    {
                        // Start from where we left off in this bucket if we get the next page
                        nextPageState = CreatePagingState(buckets, bucketIndex, Convert.ToBase64String(rows.PagingState));
                    }
                    else if (bucketIndex != buckets.Length - 1)
                    {
                        // Start from the beginning of the next bucket since we're out of rows in this one
                        nextPageState = CreatePagingState(buckets, bucketIndex + 1, string.Empty);
                    }

                    break;
                }

                bucketIndex++;
            }
            
            return new LatestVideos
            {
                Videos = results,
                PagingState = nextPageState
            };
        }

        /// <summary>
        /// Gets a page of videos for a particular user.
        /// </summary>
        public async Task<UserVideos> GetUserVideos(GetUserVideos getVideos)
        {
            // Figure out if we're getting first page or subsequent page
            PreparedStatement preparedStatement = await _statementCache.NoContext.GetOrAddAsync("SELECT * FROM user_videos WHERE userid = ?");
            IStatement boundStatement = preparedStatement.Bind(getVideos.UserId)
                                                         .SetAutoPage(false)
                                                         .SetPageSize(getVideos.PageSize);

            // The initial query won't have a paging state, but subsequent calls should if there are more pages
            if (string.IsNullOrEmpty(getVideos.PagingState) == false)
                boundStatement.SetPagingState(Convert.FromBase64String(getVideos.PagingState));

            RowSet rows = await _session.ExecuteAsync(boundStatement).ConfigureAwait(false);
            return new UserVideos
            {
                UserId = getVideos.UserId,
                Videos = rows.Select(MapRowToVideoPreview).ToList(),
                PagingState = rows.PagingState != null && rows.PagingState.Length > 0 ? Convert.ToBase64String(rows.PagingState) : null
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

        /// <summary>
        /// Creates a string representation of the paging state for the GetLatestVideos query from the inputs provided.
        /// </summary>
        private static string CreatePagingState(string[] buckets, int bucketIndex, string rowsPagingState)
        {
            return string.Format("{0}{1}{2}", string.Join(string.Empty, buckets), bucketIndex, rowsPagingState);
        }

        /// <summary>
        /// Tries to parse a paging state string created by the CreatePagingState method into the constituent parts.
        /// </summary>
        private static bool TryParsePagingState(string pagingState, out string[] buckets, out int bucketIndex, out string rowsPagingState)
        {
            buckets = new string[] { };
            bucketIndex = 0;
            rowsPagingState = null;

            if (string.IsNullOrEmpty(pagingState))
                return false;

            // Use Regex to parse string (should be 8 buckets in yyyyMMdd format, followed by 1 bucket index, followed by 0 or 1 paging state string)
            Match match = ParseLatestPagingState.Match(pagingState);
            if (match.Success == false)
                return false;

            // Match group 0 will be the entire string that matched, so start at index 1
            buckets = match.Groups[1].Captures.Cast<Capture>().Select(c => c.Value).ToArray();
            bucketIndex = int.Parse(match.Groups[2].Value);
            rowsPagingState = match.Groups.Count == 4 ? match.Groups[3].Value : null;
            return true;
        }
    }
}