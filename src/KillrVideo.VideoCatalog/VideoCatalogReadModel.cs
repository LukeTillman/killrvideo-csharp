using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.Utils;
using KillrVideo.VideoCatalog.Api;
using KillrVideo.VideoCatalog.Dtos;

namespace KillrVideo.VideoCatalog
{
    /// <summary>
    /// Reads video catalog data from Cassandra.
    /// </summary>
    public class VideoCatalogReadModel : IVideoCatalogReadModel
    {
        internal const int MaxDaysInPastForLatestVideos = 7;

        private readonly ISession _session;

        private readonly AsyncLazy<PreparedStatement> _getVideo;
        private readonly AsyncLazy<PreparedStatement> _getVideoPreview;
        private readonly AsyncLazy<PreparedStatement> _getUserVideos;
        private readonly AsyncLazy<PreparedStatement> _getUserVideosPage;
        private readonly AsyncLazy<PreparedStatement> _getLatestBucket;
        private readonly AsyncLazy<PreparedStatement> _getLatestBucketPage;

        public VideoCatalogReadModel(ISession session)
        {
            if (session == null) throw new ArgumentNullException("session");
            _session = session;

            _getVideo = new AsyncLazy<PreparedStatement>(() => _session.PrepareAsync("SELECT * FROM videos WHERE videoid = ?"));
            _getVideoPreview = new AsyncLazy<PreparedStatement>(() => _session.PrepareAsync(
                "SELECT videoid, userid, added_date, name, preview_image_location FROM videos WHERE videoid = ?"));

            // Use <= when paging here because the table is sorted in reverse order by added_date
            _getUserVideos = new AsyncLazy<PreparedStatement>(() => _session.PrepareAsync(
                "SELECT * FROM user_videos WHERE userid = ? LIMIT ?"));
            _getUserVideosPage = new AsyncLazy<PreparedStatement>(() => _session.PrepareAsync(
                "SELECT * FROM user_videos WHERE userid = ? AND (added_date, videoid) <= (?, ?) LIMIT ?"));

            _getLatestBucket = new AsyncLazy<PreparedStatement>(() => _session.PrepareAsync("SELECT * FROM latest_videos WHERE yyyymmdd = ? LIMIT ?"));
            _getLatestBucketPage = new AsyncLazy<PreparedStatement>(() => _session.PrepareAsync(
                "SELECT * FROM latest_videos WHERE yyyymmdd = ? AND (added_date, videoid) <= (?, ?) LIMIT ?"));
        }

        /// <summary>
        /// Gets the details of a specific video.
        /// </summary>
        public async Task<VideoDetails> GetVideo(Guid videoId)
        {
            PreparedStatement preparedStatement = await _getVideo;
            BoundStatement boundStatement = preparedStatement.Bind(videoId);
            RowSet rows = await _session.ExecuteAsync(boundStatement);
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
            PreparedStatement prepared = await _getVideoPreview;

            // Bind multiple times to the prepared statement with each video id and execute all the gets in parallel, then await
            // the completion of all the gets
            RowSet[] rowSets = await Task.WhenAll(videoIds.Select(id => _session.ExecuteAsync(prepared.Bind(id))));

            // Flatten the rows in the rowsets to VideoPreview objects
            return rowSets.SelectMany(rowSet => rowSet, (_, row) => MapRowToVideoPreview(row));
        }

        /// <summary>
        /// Gets the X latest videos added to the site where X is the number of videos specified.
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
                    preparedStatement = await _getLatestBucketPage;
                    boundStatement = preparedStatement.Bind(bucket, getVideos.FirstVideoOnPageDate.Value, getVideos.FirstVideoOnPageVideoId.Value,
                                                            recordsStillNeeded);
                }
                else
                {
                    preparedStatement = await _getLatestBucket;
                    boundStatement = preparedStatement.Bind(bucket, recordsStillNeeded);
                }

                RowSet rows = await _session.ExecuteAsync(boundStatement);

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
                preparedStatement = await _getUserVideosPage;
                boundStatement = preparedStatement.Bind(getVideos.UserId, getVideos.FirstVideoOnPageAddedDate.Value,
                                                        getVideos.FirstVideoOnPageVideoId.Value, getVideos.PageSize);
            }
            else
            {
                preparedStatement = await _getUserVideos;
                boundStatement = preparedStatement.Bind(getVideos.UserId, getVideos.PageSize);
            }

            RowSet rows = await _session.ExecuteAsync(boundStatement);
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