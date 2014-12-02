using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.SuggestedVideos.ReadModel.Dtos;
using KillrVideo.Utils;

namespace KillrVideo.SuggestedVideos.ReadModel
{
    /// <summary>
    /// Makes video suggestions based on data in Cassandra.
    /// </summary>
    public class SuggestVideos : ISuggestVideos
    {
        private const int RelatedVideosToReturn = 4;

        private readonly ISession _session;
        private readonly TaskCache<string, PreparedStatement> _statementCache;

        public SuggestVideos(ISession session, TaskCache<string, PreparedStatement> statementCache)
        {
            if (session == null) throw new ArgumentNullException("session");
            if (statementCache == null) throw new ArgumentNullException("statementCache");
            _session = session;
            _statementCache = statementCache;
        }

        /// <summary>
        /// Gets the first 5 videos related to the specified video.
        /// </summary>
        public async Task<RelatedVideos> GetRelatedVideos(Guid videoId)
        {
            // Lookup the tags for the video
            PreparedStatement tagsForVideoPrepared = await _statementCache.NoContext.GetOrAddAsync("SELECT tags FROM videos WHERE videoid = ?");
            BoundStatement tagsForVideoBound = tagsForVideoPrepared.Bind(videoId);
            RowSet tagRows = await _session.ExecuteAsync(tagsForVideoBound);
            Row tagRow = tagRows.SingleOrDefault();
            if (tagRow == null)
                return new RelatedVideos { VideoId = videoId, Videos = Enumerable.Empty<VideoPreview>() };

            var tagsValue = tagRow.GetValue<IEnumerable<string>>("tags");
            var tags = tagsValue == null ? new List<string>() : tagsValue.ToList();

            // If there are no tags, we can't find related videos
            if (tags.Count == 0)
                return new RelatedVideos { VideoId = videoId, Videos = Enumerable.Empty<VideoPreview>() };

            var relatedVideos = new Dictionary<Guid, VideoPreview>();
            PreparedStatement videosForTagPrepared = await _statementCache.NoContext.GetOrAddAsync("SELECT * FROM videos_by_tag WHERE tag = ? LIMIT ?");

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