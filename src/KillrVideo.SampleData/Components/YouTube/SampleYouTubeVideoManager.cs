using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cassandra;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using KillrVideo.Utils;

namespace KillrVideo.SampleData.Components.YouTube
{
    /// <summary>
    /// Component responsible for retrieving random videos from YouTube.
    /// </summary>
    public class SampleYouTubeVideoManager : IManageSampleYouTubeVideos
    {
        private static readonly DateTimeOffset Epoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
        
        private const int MaxVideosPerRequest = 50;
        private const int MaxVideosPerSource = 300;

        private readonly YouTubeService _youTubeService;
        private readonly ISession _session;
        private readonly TaskCache<string, PreparedStatement> _statementCache;
        
        public SampleYouTubeVideoManager(YouTubeService youTubeService, ISession session)
        {
            if (youTubeService == null) throw new ArgumentNullException(nameof(youTubeService));
            if (session == null) throw new ArgumentNullException(nameof(session));
            _youTubeService = youTubeService;
            _session = session;

            _statementCache = new TaskCache<string, PreparedStatement>(_session.PrepareAsync);
        }

        /// <summary>
        /// Refreshes the videos cached in Cassandra for the given source.
        /// </summary>
        public Task RefreshSource(YouTubeVideoSource source)
        {
            return source.RefreshVideos(this);
        }

        /// <summary>
        /// Gets a list of unused YouTubeVideos with the page size specified.
        /// </summary>
        public async Task<List<YouTubeVideo>> GetUnusedVideos(int pageSize)
        {
            // Statement for getting unused videos from a source
            PreparedStatement prepared =
                await _statementCache.NoContext.GetOrAddAsync("SELECT * FROM sample_data_youtube_videos WHERE sourceid = ?");
            
            // Iterate the list of sources in random order
            var random = new Random();
            var indexes = Enumerable.Range(0, YouTubeVideoSource.All.Count).OrderBy(_ => random.Next());

            // Build a list of unused videos from the available sources in random order
            var unusedVideos = new List<YouTubeVideo>();
            foreach (int idx in indexes)
            {
                YouTubeVideoSource source = YouTubeVideoSource.All[idx];

                // Use automatic paging to page through all the videos from the source
                BoundStatement bound = prepared.Bind(source.UniqueId);
                bound.SetPageSize(MaxVideosPerRequest);
                RowSet rowSet = await _session.ExecuteAsync(bound).ConfigureAwait(false);

                foreach (Row row in rowSet)
                {
                    var used = row.GetValue<bool?>("used");
                    if (used == false || used == null)
                        unusedVideos.Add(MapToYouTubeVideo(row, source, random));
                    
                    // If we've got enough videos, return them
                    if (unusedVideos.Count == pageSize)
                        return unusedVideos;
                }
            }
            
            // We were unable to fill the quota, so throw
            throw new InvalidOperationException("Unable to get unused videos.  Time to add more sources?");
        }

        /// <summary>
        /// Marks the YouTube video specified as used.
        /// </summary>
        public async Task MarkVideoAsUsed(YouTubeVideo video)
        {
            // Mark all videos as used using queries in parallel
            PreparedStatement prepared = await _statementCache.NoContext.GetOrAddAsync(
                "UPDATE sample_data_youtube_videos SET used = true WHERE sourceid = ? AND published_at = ? AND youtube_video_id = ?");

            await _session.ExecuteAsync(prepared.Bind(video.Source.UniqueId, video.PublishedAt, video.YouTubeVideoId)).ConfigureAwait(false);
        }

        /// <summary>
        /// Refreshes the cached data in Cassandra for a given YouTube channel.
        /// </summary>
        internal async Task RefreshChannel(YouTubeVideoSource.VideosFromChannel channelSource)
        {
            // Since channel lists are sorted by date, try to be smart and see what the date is of the latest video we have for the channel
            PreparedStatement prepared =
                await _statementCache.NoContext.GetOrAddAsync("SELECT published_at FROM sample_data_youtube_videos WHERE sourceid = ? LIMIT 1");
            RowSet rowSet = await _session.ExecuteAsync(prepared.Bind(channelSource.UniqueId)).ConfigureAwait(false);
            Row row = rowSet.SingleOrDefault();
            DateTimeOffset newestVideoWeHave = row?.GetValue<DateTimeOffset>("published_at") ?? Epoch;

            // Statement for inserting the video into the sample table
            PreparedStatement preparedInsert = await _statementCache.NoContext.GetOrAddAsync(
                "INSERT INTO sample_data_youtube_videos (sourceid, published_at, youtube_video_id, name, description) VALUES (?, ?, ?, ?, ?)");

            bool getMoreVideos = true;
            string nextPageToken = null;
            var insertTasks = new List<Task>();
            do
            {
                // Create search by channel request
                SearchResource.ListRequest searchRequest = _youTubeService.Search.List("snippet");
                searchRequest.MaxResults = MaxVideosPerRequest;
                searchRequest.ChannelId = channelSource.ChannelId;
                searchRequest.Type = "video";
                searchRequest.Order = SearchResource.ListRequest.OrderEnum.Date;
                if (string.IsNullOrEmpty(nextPageToken) == false)
                    searchRequest.PageToken = nextPageToken;

                // Get the results and insert as rows in Cassandra
                SearchListResponse searchResults = await searchRequest.ExecuteAsync().ConfigureAwait(false);
                foreach (SearchResult searchResult in searchResults.Items)
                {
                    DateTimeOffset publishedAt = searchResult.Snippet.PublishedAt.HasValue
                                                     ? searchResult.Snippet.PublishedAt.Value.ToUniversalTime()
                                                     : Epoch;

                    // If we've reached the max or the video we're going to insert is older than our newest video, no need to continue
                    if (insertTasks.Count >= MaxVideosPerSource || publishedAt < newestVideoWeHave)
                    {
                        getMoreVideos = false;
                        break;
                    }

                    Task<RowSet> insertTask = _session.ExecuteAsync(preparedInsert.Bind(channelSource.UniqueId, publishedAt, searchResult.Id.VideoId,
                                                                                        searchResult.Snippet.Title, searchResult.Snippet.Description));
                    insertTasks.Add(insertTask);
                }

                // If we don't have a next page, we can bail
                nextPageToken = searchResults.NextPageToken;
                if (string.IsNullOrEmpty(nextPageToken))
                    getMoreVideos = false;

            } while (getMoreVideos);

            // Wait for any insert tasks to finish
            if (insertTasks.Count > 0)
                await Task.WhenAll(insertTasks).ConfigureAwait(false);
        }

        /// <summary>
        /// Refreshes the cached data in Cassandra for a given YouTube keyword search.
        /// </summary>
        internal async Task RefreshKeywords(YouTubeVideoSource.VideosWithKeyword keywordSource)
        {
            // Statement for inserting the video into the sample table
            PreparedStatement preparedInsert = await _statementCache.NoContext.GetOrAddAsync(
                "INSERT INTO sample_data_youtube_videos (sourceid, published_at, youtube_video_id, name, description) VALUES (?, ?, ?, ?, ?)");

            bool getMoreVideos = true;
            string nextPageToken = null;
            var insertTasks = new List<Task>();
            do
            {
                // Create search by keywords request
                SearchResource.ListRequest searchRequest = _youTubeService.Search.List("snippet");
                searchRequest.MaxResults = MaxVideosPerRequest;
                searchRequest.Q = keywordSource.SearchTerms;
                searchRequest.Type = "video";
                if (string.IsNullOrEmpty(nextPageToken) == false)
                    searchRequest.PageToken = nextPageToken;

                // Get the results and insert as rows in Cassandra
                SearchListResponse searchResults = await searchRequest.ExecuteAsync().ConfigureAwait(false);
                foreach (SearchResult searchResult in searchResults.Items)
                {
                    // If we've reached the max, no need to continue
                    if (insertTasks.Count >= MaxVideosPerSource)
                    {
                        getMoreVideos = false;
                        break;
                    }

                    DateTimeOffset publishedAt = searchResult.Snippet.PublishedAt.HasValue
                                                     ? searchResult.Snippet.PublishedAt.Value.ToUniversalTime()
                                                     : Epoch;

                    Task<RowSet> insertTask = _session.ExecuteAsync(preparedInsert.Bind(keywordSource.UniqueId, publishedAt, searchResult.Id.VideoId,
                                                                                        searchResult.Snippet.Title, searchResult.Snippet.Description));
                    insertTasks.Add(insertTask);
                }

                // If we don't have a next page, we can bail
                nextPageToken = searchResults.NextPageToken;
                if (string.IsNullOrEmpty(nextPageToken))
                    getMoreVideos = false;

            } while (getMoreVideos);

            // Wait for any insert tasks to finish
            if (insertTasks.Count > 0)
                await Task.WhenAll(insertTasks).ConfigureAwait(false);
        }

        /// <summary>
        /// Refreshes the cached data in Cassandra for a given YouTube playlist.
        /// </summary>
        internal async Task RefreshPlaylist(YouTubeVideoSource.VideosFromPlaylist playlistSource)
        {
            // Statement for inserting the video into the sample table
            PreparedStatement preparedInsert = await _statementCache.NoContext.GetOrAddAsync(
                "INSERT INTO sample_data_youtube_videos (sourceid, published_at, youtube_video_id, name, description) VALUES (?, ?, ?, ?, ?)");

            bool getMoreVideos = true;
            string nextPageToken = null;
            var insertTasks = new List<Task>();
            do
            {
                // Create playlist items request
                PlaylistItemsResource.ListRequest playlistRequest = _youTubeService.PlaylistItems.List("snippet");
                playlistRequest.MaxResults = MaxVideosPerRequest;
                playlistRequest.PlaylistId = playlistSource.PlaylistId;
                if (string.IsNullOrEmpty(nextPageToken) == false)
                    playlistRequest.PageToken = nextPageToken;

                // Get the results and insert as rows in Cassandra
                PlaylistItemListResponse playlistItems = await playlistRequest.ExecuteAsync().ConfigureAwait(false);
                foreach (var playlistItem in playlistItems.Items)
                {
                    // If we've reached the max, no need to continue
                    if (insertTasks.Count >= MaxVideosPerSource)
                    {
                        getMoreVideos = false;
                        break;
                    }

                    DateTimeOffset publishedAt = playlistItem.Snippet.PublishedAt.HasValue
                                                     ? playlistItem.Snippet.PublishedAt.Value.ToUniversalTime()
                                                     : Epoch;

                    Task<RowSet> insertTask =
                        _session.ExecuteAsync(preparedInsert.Bind(playlistSource.UniqueId, publishedAt, playlistItem.Snippet.ResourceId.VideoId,
                                                                  playlistItem.Snippet.Title, playlistItem.Snippet.Description));
                    insertTasks.Add(insertTask);
                }

                // If we don't have a next page, we can bail
                nextPageToken = playlistItems.NextPageToken;
                if (string.IsNullOrEmpty(nextPageToken))
                    getMoreVideos = false;

            } while (getMoreVideos);

            // Wait for any insert tasks to finish
            if (insertTasks.Count > 0)
                await Task.WhenAll(insertTasks).ConfigureAwait(false);
        }
        
        private static YouTubeVideo MapToYouTubeVideo(Row row, YouTubeVideoSource source, Random random)
        {
            // Map to video
            var video = new YouTubeVideo
            {
                Source = source,
                PublishedAt = row.GetValue<DateTimeOffset>("published_at"),
                YouTubeVideoId = row.GetValue<string>("youtube_video_id"),
                Name = row.GetValue<string>("name"),
                Description = row.GetValue<string>("description")
            };

            // Choose some tags for the video (this isn't a fantastic way to do this, but since it's sample data, it will do)
            var tags = new HashSet<string>();
            int maxTags = random.Next(3, 6);

            string lowerName = video.Name.ToLowerInvariant();
            string lowerDescription = video.Description.ToLowerInvariant();

            foreach (string possibleTag in source.PossibleTags)
            {
                if (lowerName.Contains(possibleTag) || lowerDescription.Contains(possibleTag))
                    tags.Add(possibleTag);

                if (tags.Count == maxTags)
                    break;
            }

            // If we didn't get any tags, just pick some random ones specific to the source
            if (tags.Count == 0)
            {
                foreach (string tag in source.SourceTags.Take(maxTags))
                    tags.Add(tag);
            }

            // Return video with suggested tags
            video.SuggestedTags = tags;
            return video;
        }
    }
}
