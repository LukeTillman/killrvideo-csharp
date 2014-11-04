using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.Data.Videos.Dtos;

namespace KillrVideo.Data.Videos
{
    /// <summary>
    /// Handles reading data from Cassandra for videos.
    /// </summary>
    public class VideoReadModel : IVideoReadModel
    {
        private readonly ISession _session;

        internal const int MaxDaysInPastForLatestVideos = 7;
        private const int RelatedVideosToReturn = 5;

        private readonly AsyncLazy<PreparedStatement> _getVideo;
        private readonly AsyncLazy<PreparedStatement> _getVideoPreview; 
        private readonly AsyncLazy<PreparedStatement> _getVideoRating;
        private readonly AsyncLazy<PreparedStatement> _getVideoRatingForUser;
        private readonly AsyncLazy<PreparedStatement> _getUserVideos;
        private readonly AsyncLazy<PreparedStatement> _getUserVideosPage;
        private readonly AsyncLazy<PreparedStatement> _getLatestBucket;
        private readonly AsyncLazy<PreparedStatement> _getLatestBucketPage;
        private readonly AsyncLazy<PreparedStatement> _getTagsForVideo;
        private readonly AsyncLazy<PreparedStatement> _getVideosForTag;
        private readonly AsyncLazy<PreparedStatement> _getVideosForTagPage;
        private readonly AsyncLazy<PreparedStatement> _getTagsStartingWith;
        


        public VideoReadModel(ISession session)
        {
            if (session == null) throw new ArgumentNullException("session");
            _session = session;

            // Reusable prepared statements
            _getVideo = new AsyncLazy<PreparedStatement>(() => _session.PrepareAsync("SELECT * FROM videos WHERE videoid = ?"));

            _getVideoPreview = new AsyncLazy<PreparedStatement>(() => _session.PrepareAsync(
                "SELECT videoid, userid, added_date, name, preview_image_location FROM videos WHERE videoid = ?"));

            _getVideoRating = new AsyncLazy<PreparedStatement>(() => _session.PrepareAsync("SELECT * FROM video_ratings WHERE videoid = ?"));
            _getVideoRatingForUser = new AsyncLazy<PreparedStatement>(() => _session.PrepareAsync(
                "SELECT rating FROM video_ratings_by_user WHERE videoid = ? AND userid = ?"));

            // Use <= when paging here because the table is sorted in reverse order by added_date
            _getUserVideos = new AsyncLazy<PreparedStatement>(() => _session.PrepareAsync(
                "SELECT * FROM user_videos WHERE userid = ? LIMIT ?"));
            _getUserVideosPage = new AsyncLazy<PreparedStatement>(() => _session.PrepareAsync(
                "SELECT * FROM user_videos WHERE userid = ? AND (added_date, videoid) <= (?, ?) LIMIT ?"));

            _getLatestBucket = new AsyncLazy<PreparedStatement>(() => _session.PrepareAsync("SELECT * FROM latest_videos WHERE yyyymmdd = ? LIMIT ?"));
            _getLatestBucketPage = new AsyncLazy<PreparedStatement>(() => _session.PrepareAsync(
                "SELECT * FROM latest_videos WHERE yyyymmdd = ? AND (added_date, videoid) <= (?, ?) LIMIT ?"));
            _getTagsForVideo = new AsyncLazy<PreparedStatement>(() => _session.PrepareAsync("SELECT tags FROM videos WHERE videoid = ?"));

            // One for getting the first page, one for getting subsequent pages
            _getVideosForTag = new AsyncLazy<PreparedStatement>(() => _session.PrepareAsync("SELECT * FROM videos_by_tag WHERE tag = ? LIMIT ?"));
            _getVideosForTagPage =
                new AsyncLazy<PreparedStatement>(() => _session.PrepareAsync("SELECT * FROM videos_by_tag WHERE tag = ? AND videoid >= ? LIMIT ?"));

            _getTagsStartingWith =
                new AsyncLazy<PreparedStatement>(() => _session.PrepareAsync("SELECT tag FROM tags_by_letter WHERE first_letter = ? AND tag >= ? LIMIT ?"));
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
            RowSet[] rowSets = await Task.WhenAll(videoIds.Select(id => prepared.Bind(id))
                                                          .Select(_session.ExecuteAsync));

            // Flatten the rows in the rowsets to VideoPreview objects
            return rowSets.SelectMany(rowSet => rowSet, (_, row) => MapRowToVideoPreview(row));
        }

        /// <summary>
        /// Gets the current rating stats for the specified video.
        /// </summary>
        public async Task<VideoRating> GetRating(Guid videoId)
        {
            PreparedStatement preparedStatement = await _getVideoRating;
            BoundStatement boundStatement = preparedStatement.Bind(videoId);
            RowSet rows = await _session.ExecuteAsync(boundStatement);

            // Use SingleOrDefault here because it's possible a video doesn't have any ratings yet and thus has no record
            return MapRowToVideoRating(rows.SingleOrDefault(), videoId);
        }

        /// <summary>
        /// Gets the rating given by a user for a specific video.  Will return 0 for the rating if the user hasn't rated the video.
        /// </summary>
        public async Task<UserVideoRating> GetRatingFromUser(Guid videoId, Guid userId)
        {
            PreparedStatement preparedStatement = await _getVideoRatingForUser;
            BoundStatement boundStatement = preparedStatement.Bind(videoId, userId);
            RowSet rows = await _session.ExecuteAsync(boundStatement);

            // We may or may not have a rating
            Row row = rows.SingleOrDefault();
            return new UserVideoRating
            {
                VideoId = videoId, 
                UserId = userId, 
                Rating = row == null ? 0 : row.GetValue<int>("rating")
            };
        }

        /// <summary>
        /// Gets the X latest videos added to the site where X is the number of videos specified.
        /// </summary>
        public async Task<LatestVideos> GetLastestVideos(GetLatestVideos getVideos)
        {
            // We may need multiple queries to fill the quota
            var results = new List<VideoPreview>();
            DateTimeOffset now = DateTimeOffset.UtcNow;
            int? firstVideoOnPageDaysInPast = getVideos.FirstVideoOnPageDate.HasValue && getVideos.FirstVideoOnPageVideoId.HasValue
                                                  ? Convert.ToInt32(Math.Floor(now.Subtract(getVideos.FirstVideoOnPageDate.Value).TotalDays))
                                                  : (int?) null;

            int daysInPast = firstVideoOnPageDaysInPast.HasValue ? firstVideoOnPageDaysInPast.Value : 0;
            int numberOfVideos = getVideos.PageSize;
            
            // TODO: Run queries in parallel instead of sequentially?
            while (daysInPast <= MaxDaysInPastForLatestVideos)
            {
                int recordsStillNeeded = numberOfVideos - results.Count;

                // Get the bucket for the current number of days back we're processing
                string bucket = now.Subtract(TimeSpan.FromDays(daysInPast)).ToString("yyyyMMdd");

                // If we're processing a paged request, use the appropriate statement
                PreparedStatement preparedStatement;
                IStatement boundStatement;
                if (firstVideoOnPageDaysInPast.HasValue && firstVideoOnPageDaysInPast.Value == daysInPast)
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
                if (results.Count >= numberOfVideos)
                    break;

                // Try another day back
                daysInPast++;
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
        /// Gets the first 5 videos related to the specified video.
        /// </summary>
        public async Task<RelatedVideos> GetRelatedVideos(Guid videoId)
        {
            // Lookup the tags for the video
            PreparedStatement tagsForVideoPrepared = await _getTagsForVideo;
            BoundStatement tagsForVideoBound = tagsForVideoPrepared.Bind(videoId);
            RowSet tagRows = await _session.ExecuteAsync(tagsForVideoBound);
            Row tagRow = tagRows.SingleOrDefault();
            if (tagRow == null)
                return new RelatedVideos {VideoId = videoId, Videos = Enumerable.Empty<VideoPreview>()};

            var tagsValue = tagRow.GetValue<IEnumerable<string>>("tags");
            var tags = tagsValue == null ? new List<string>() : tagsValue.ToList();
            
            // If there are no tags, we can't find related videos
            if (tags.Count == 0)
                return new RelatedVideos {VideoId = videoId, Videos = Enumerable.Empty<VideoPreview>()};

            var relatedVideos = new Dictionary<Guid, VideoPreview>();
            PreparedStatement videosForTagPrepared = await _getVideosForTag;

            var inFlightQueries = new List<Task<RowSet>>();
            for (var i = 0; i < tags.Count; i++)
            {
                // Use the number of results we ultimately want * 2 when querying so that we can account for potentially having to filter 
                // out the video Id we're using as the basis for the query as well as duplicates
                const int pageSize = RelatedVideosToReturn * 2;

                // Kick off a query for each tag and track them in the inflight requests list
                string tag = tags[i];
                IStatement query = videosForTagPrepared.Bind(tag, pageSize);
                inFlightQueries.Add(_session.ExecuteAsync(query));
                
                // Every third query, or if this is the last tag, wait on all the query results
                if (inFlightQueries.Count == 3 || i == tags.Count - 1)
                {
                    RowSet[] results = await Task.WhenAll(inFlightQueries);

                    foreach (RowSet rowSet in results)
                    {
                        foreach (Row row in rowSet)
                        {
                            VideoPreview preview = MapRowToVideoPreview(row);

                            // Skip self
                            if (preview.VideoId == videoId)
                                continue;

                            // Skip videos we already have in the results
                            if (relatedVideos.ContainsKey(preview.VideoId))
                                continue;

                            // Add to results
                            relatedVideos.Add(preview.VideoId, preview);

                            // If we've got enough, no reason to continue
                            if (relatedVideos.Count >= RelatedVideosToReturn)
                                break;
                        }

                        // If we've got enough, no reason to continue
                        if (relatedVideos.Count >= RelatedVideosToReturn)
                            break;
                    }
                    
                    // See if we've got enough results now to fulfill our requirement
                    if (relatedVideos.Count >= RelatedVideosToReturn)
                        break;

                    // We don't have enough yet, so reset the inflight requests to allow another batch of tags to be queried
                    inFlightQueries.Clear();
                }
            }

            return new RelatedVideos
            {
                VideoId = videoId,
                Videos = relatedVideos.Values
            };
        }

        /// <summary>
        /// Gets a page of videos by tag.
        /// </summary>
        public async Task<VideosByTag> GetVideosByTag(GetVideosByTag getVideos)
        {
            // If the first video id for the page was specified, use the query for a subsequent page, otherwise use the query for the first page
            PreparedStatement preparedStatement;
            IStatement boundStatement;
            if (getVideos.FirstVideoOnPageVideoId == null)
            {
                preparedStatement = await _getVideosForTag;
                boundStatement = preparedStatement.Bind(getVideos.Tag, getVideos.PageSize);
            }
            else
            {
                preparedStatement = await _getVideosForTagPage;
                boundStatement = preparedStatement.Bind(getVideos.Tag, getVideos.FirstVideoOnPageVideoId.Value, getVideos.PageSize);
            }
            
            RowSet rows = await _session.ExecuteAsync(boundStatement);
            return new VideosByTag
            {
                Tag = getVideos.Tag,
                Videos = rows.Select(MapRowToVideoPreview).ToList()
            };
        }

        /// <summary>
        /// Gets a list of tags starting with specified text.
        /// </summary>
        public async Task<TagsStartingWith> GetTagsStartingWith(GetTagsStartingWith getTags)
        {
            string firstLetter = getTags.TagStartsWith.Substring(0, 1);
            PreparedStatement preparedStatement = await _getTagsStartingWith;
            BoundStatement boundStatement = preparedStatement.Bind(firstLetter, getTags.TagStartsWith, getTags.PageSize);
            RowSet rows = await _session.ExecuteAsync(boundStatement);
            return new TagsStartingWith
            {
                TagStartsWith = getTags.TagStartsWith,
                Tags = rows.Select(row => row.GetValue<string>("tag")).ToList()
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
        /// Maps a row to a VideoRating object.
        /// </summary>
        private static VideoRating MapRowToVideoRating(Row row, Guid videoId)
        {
            // If we get null, just return an object with 0s as the rating tallys
            if (row == null)
                return new VideoRating {VideoId = videoId, RatingsCount = 0, RatingsTotal = 0};

            return new VideoRating
            {
                VideoId = videoId,
                RatingsCount = row.GetValue<long>("rating_counter"),
                RatingsTotal = row.GetValue<long>("rating_total")
            };
        }
    }
}
