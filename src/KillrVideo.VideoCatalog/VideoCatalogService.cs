using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Cassandra;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using KillrVideo.Cassandra;
using KillrVideo.MessageBus;
using KillrVideo.Protobuf;
using KillrVideo.Protobuf.Services;
using KillrVideo.VideoCatalog.Events;

namespace KillrVideo.VideoCatalog
{
    /// <summary>
    /// An implementation of the video catalog service that stores catalog data in Cassandra and publishes events on a message bus.
    /// </summary>
    [Export(typeof(IGrpcServerService))]
    public class VideoCatalogServiceImpl : VideoCatalogService.IVideoCatalogService, IGrpcServerService
    {
        public static readonly int LatestVideosTtlSeconds = Convert.ToInt32(TimeSpan.FromDays(MaxDaysInPastForLatestVideos).TotalSeconds);

        private const int MaxDaysInPastForLatestVideos = 7;
        private static readonly Regex ParseLatestPagingState = new Regex("([0-9]{8}){8}([0-9]{1})(.*)", RegexOptions.Compiled | RegexOptions.Singleline);
        
        private readonly ISession _session;
        private readonly IBus _bus;
        private readonly PreparedStatementCache _statementCache;

        public VideoCatalogServiceImpl(ISession session, PreparedStatementCache statementCache, IBus bus)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (statementCache == null) throw new ArgumentNullException(nameof(statementCache));
            if (bus == null) throw new ArgumentNullException(nameof(bus));
            _session = session;
            _statementCache = statementCache;
            _bus = bus;
        }

        /// <summary>
        /// Convert this instance to a ServerServiceDefinition that can be run on a Grpc server.
        /// </summary>
        public ServerServiceDefinition ToServerServiceDefinition()
        {
            return VideoCatalogService.BindService(this);
        }

        /// <summary>
        /// Submits an uploaded video to the catalog.
        /// </summary>
        public async Task<SubmitUploadedVideoResponse> SubmitUploadedVideo(SubmitUploadedVideoRequest request, ServerCallContext context)
        {
            var timestamp = DateTimeOffset.UtcNow;

            // Store the information we have now in Cassandra
            PreparedStatement[] prepared = await _statementCache.GetOrAddAllAsync(
                "INSERT INTO videos (videoid, userid, name, description, tags, location_type, added_date) VALUES (?, ?, ?, ?, ?, ?, ?)",
                "INSERT INTO user_videos (userid, added_date, videoid, name) VALUES (?, ?, ?, ?)"
            );

            var batch = new BatchStatement();

            batch.Add(prepared[0].Bind(request.VideoId.ToGuid(), request.UserId.ToGuid(), request.Name, request.Description,
                                       request.Tags.ToArray(), (int) VideoLocationType.UPLOAD, timestamp))
                 .Add(prepared[1].Bind(request.UserId.ToGuid(), timestamp, request.VideoId.ToGuid(), request.Name))
                 .SetTimestamp(timestamp);

            await _session.ExecuteAsync(batch).ConfigureAwait(false);

            // Tell the world we've accepted an uploaded video (it hasn't officially been added until we get a location for the
            // video playback and thumbnail)
            await _bus.Publish(new UploadedVideoAccepted
            {
                VideoId = request.VideoId,
                UploadUrl = request.UploadUrl,
                Timestamp = timestamp.ToTimestamp()
            }).ConfigureAwait(false);

            return new SubmitUploadedVideoResponse();
        }

        /// <summary>
        /// Submits a YouTube video to the catalog.
        /// </summary>
        public async Task<SubmitYouTubeVideoResponse> SubmitYouTubeVideo(SubmitYouTubeVideoRequest request, ServerCallContext context)
        {
            // Use a batch to insert the YouTube video into multiple tables
            PreparedStatement[] prepared = await _statementCache.GetOrAddAllAsync(
                "INSERT INTO videos (videoid, userid, name, description, location, preview_image_location, tags, added_date, location_type) " +
                "VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)",
                "INSERT INTO user_videos (userid, added_date, videoid, name, preview_image_location) VALUES (?, ?, ?, ?, ?)",
                "INSERT INTO latest_videos (yyyymmdd, added_date, videoid, userid, name, preview_image_location) VALUES (?, ?, ?, ?, ?, ?) USING TTL ?");

            // Calculate date-related info and location/thumbnail for YouTube video
            var addDate = DateTimeOffset.UtcNow;
            string yyyymmdd = addDate.ToString("yyyyMMdd");

            string location = request.YouTubeVideoId;      // TODO: Store URL instead of ID?
            string previewImageLocation = $"//img.youtube.com/vi/{request.YouTubeVideoId}/hqdefault.jpg";

            var batch = new BatchStatement();
            batch.Add(prepared[0].Bind(request.VideoId.ToGuid(), request.UserId.ToGuid(), request.Name, request.Description, location,
                                       previewImageLocation, request.Tags.ToArray(), addDate, (int) VideoLocationType.YOUTUBE))
                 .Add(prepared[1].Bind(request.UserId.ToGuid(), addDate, request.VideoId.ToGuid(), request.Name, previewImageLocation))
                 .Add(prepared[2].Bind(yyyymmdd, addDate, request.VideoId.ToGuid(), request.UserId.ToGuid(), request.Name, previewImageLocation, LatestVideosTtlSeconds))
                 .SetTimestamp(addDate);
            
            // Send the batch to Cassandra
            await _session.ExecuteAsync(batch).ConfigureAwait(false);

            // Tell the world about the new YouTube video
            var message = new YouTubeVideoAdded
            {
                VideoId = request.VideoId,
                UserId = request.UserId,
                Name = request.Name,
                Description = request.Description,
                Location = location,
                PreviewImageLocation = previewImageLocation,
                AddedDate = addDate.ToTimestamp(),
                Timestamp = addDate.ToTimestamp()
            };
            message.Tags.Add(request.Tags);
            await _bus.Publish(message).ConfigureAwait(false);

            return new SubmitYouTubeVideoResponse();
        }

        /// <summary>
        /// Gets the details of a specific video.
        /// </summary>
        public async Task<GetVideoResponse> GetVideo(GetVideoRequest request, ServerCallContext context)
        {
            PreparedStatement preparedStatement = await _statementCache.GetOrAddAsync("SELECT * FROM videos WHERE videoid = ?");
            RowSet rows = await _session.ExecuteAsync(preparedStatement.Bind(request.VideoId.ToGuid())).ConfigureAwait(false);
            Row row = rows.SingleOrDefault();
            if (row == null)
                return null; // TODO: Throw exception? How to do error responses with grpc?

            var response = new GetVideoResponse
            {
                VideoId = row.GetValue<Guid>("videoid").ToUuid(),
                UserId = row.GetValue<Guid>("userid").ToUuid(),
                Name = row.GetValue<string>("name"),
                Description = row.GetValue<string>("description"),
                Location = row.GetValue<string>("location"),
                LocationType = (VideoLocationType) row.GetValue<int>("location_type"),
                AddedDate = row.GetValue<DateTimeOffset>("added_date").ToTimestamp()
            };

            var tags = row.GetValue<IEnumerable<string>>("tags");
            if (tags != null)
                response.Tags.Add(tags);

            return response;
        }

        /// <summary>
        /// Gets a limited number of video preview data by video id.
        /// </summary>
        public async Task<GetVideoPreviewsResponse> GetVideoPreviews(GetVideoPreviewsRequest request, ServerCallContext context)
        {
            var response = new GetVideoPreviewsResponse();

            if (request.VideoIds == null || request.VideoIds.Count == 0)
                return response;

            // Since we're doing a multi-get here, limit the number of previews to 20 to try and enforce some
            // performance sanity.  If we ever needed to do more than that, we might think about a different
            // data model that doesn't involve a multi-get.
            if (request.VideoIds.Count > 20) throw new ArgumentOutOfRangeException(nameof(request.VideoIds), "Cannot fetch more than 20 videos at once.");

            // As an example, let's do the multi-get using multiple statements at the driver level.  For an example of doing this at
            // the CQL level with an IN() clause, see UserManagement's GetUserProfiles
            PreparedStatement prepared = await _statementCache.GetOrAddAsync(
                "SELECT videoid, userid, added_date, name, preview_image_location FROM videos WHERE videoid = ?");

            // Bind multiple times to the prepared statement with each video id and execute all the gets in parallel, then await
            // the completion of all the gets
            RowSet[] rowSets = await Task.WhenAll(request.VideoIds.Select(id => _session.ExecuteAsync(prepared.Bind(id.ToGuid())))).ConfigureAwait(false);

            // Flatten the rows in the rowsets to VideoPreview objects
            response.VideoPreviews.Add(rowSets.SelectMany(rowSet => rowSet, (_, row) => MapRowToVideoPreview(row)));
            return response;
        }

        /// <summary>
        /// Gets the latest videos added to the site.
        /// </summary>
        public async Task<GetLatestVideoPreviewsResponse> GetLatestVideoPreviews(GetLatestVideoPreviewsRequest request, ServerCallContext context)
        {
            string[] buckets;
            int bucketIndex;
            string rowPagingState;

            // See if we have paging state from a previous page of videos
            if (TryParsePagingState(request.PagingState, out buckets, out bucketIndex, out rowPagingState) == false)
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
            string nextPageState = string.Empty;

            PreparedStatement preparedStatement = await _statementCache.GetOrAddAsync("SELECT * FROM latest_videos WHERE yyyymmdd = ?");

            // TODO: Run queries in parallel?
            while (bucketIndex < buckets.Length)
            {
                int recordsStillNeeded = request.PageSize - results.Count;
                string bucket = buckets[bucketIndex];

                // Get a page of records but don't automatically load more pages when enumerating the RowSet
                
                IStatement boundStatement = preparedStatement.Bind(bucket)
                                                             .SetAutoPage(false)
                                                             .SetPageSize(recordsStillNeeded);

                // Start from where we left off in this bucket
                if (string.IsNullOrEmpty(rowPagingState) == false)
                    boundStatement.SetPagingState(Convert.FromBase64String(rowPagingState));

                RowSet rows = await _session.ExecuteAsync(boundStatement).ConfigureAwait(false);
                results.AddRange(rows.Select(MapRowToVideoPreview));

                // See if we can stop querying
                if (results.Count == request.PageSize)
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

            var response = new GetLatestVideoPreviewsResponse { PagingState = nextPageState };
            response.VideoPreviews.Add(results);
            return response;
        }

        /// <summary>
        /// Gets a page of videos for a particular user.
        /// </summary>
        public async Task<GetUserVideoPreviewsResponse> GetUserVideoPreviews(GetUserVideoPreviewsRequest request, ServerCallContext context)
        {
            // Figure out if we're getting first page or subsequent page
            PreparedStatement preparedStatement = await _statementCache.GetOrAddAsync("SELECT * FROM user_videos WHERE userid = ?");
            IStatement boundStatement = preparedStatement.Bind(request.UserId.ToGuid())
                                                         .SetAutoPage(false)
                                                         .SetPageSize(request.PageSize);

            // The initial query won't have a paging state, but subsequent calls should if there are more pages
            if (string.IsNullOrEmpty(request.PagingState) == false)
                boundStatement.SetPagingState(Convert.FromBase64String(request.PagingState));

            RowSet rows = await _session.ExecuteAsync(boundStatement).ConfigureAwait(false);
            var response = new GetUserVideoPreviewsResponse
            {
                UserId = request.UserId,
                PagingState = rows.PagingState != null && rows.PagingState.Length > 0 ? Convert.ToBase64String(rows.PagingState) : string.Empty
            };

            response.VideoPreviews.Add(rows.Select(MapRowToVideoPreview));
            return response;
        }

        /// <summary>
        /// Maps a row to a VideoPreview object.
        /// </summary>
        private static VideoPreview MapRowToVideoPreview(Row row)
        {
            return new VideoPreview
            {
                VideoId = row.GetValue<Guid>("videoid").ToUuid(),
                AddedDate = row.GetValue<DateTimeOffset>("added_date").ToTimestamp(),
                Name = row.GetValue<string>("name"),
                PreviewImageLocation = row.GetValue<string>("preview_image_location"),
                UserId = row.GetValue<Guid>("userid").ToUuid()
            };
        }

        /// <summary>
        /// Creates a string representation of the paging state for the GetLatestVideos query from the inputs provided.
        /// </summary>
        private static string CreatePagingState(string[] buckets, int bucketIndex, string rowsPagingState)
        {
            return $"{string.Join(string.Empty, buckets)}{bucketIndex}{rowsPagingState}";
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