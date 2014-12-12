using System;
using System.Linq;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.Search.Dtos;
using KillrVideo.Utils;

namespace KillrVideo.Search
{
    /// <summary>
    /// Searches for videos by tag in Cassandra.
    /// </summary>
    public class SearchVideosByTag : ISearchVideosByTag
    {
        private readonly ISession _session;
        private readonly TaskCache<string, PreparedStatement> _statementCache;

        public SearchVideosByTag(ISession session, TaskCache<string, PreparedStatement> statementCache)
        {
            if (session == null) throw new ArgumentNullException("session");
            if (statementCache == null) throw new ArgumentNullException("statementCache");
            _session = session;
            _statementCache = statementCache;
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
                preparedStatement = await _statementCache.NoContext.GetOrAddAsync("SELECT * FROM videos_by_tag WHERE tag = ? LIMIT ?");
                boundStatement = preparedStatement.Bind(getVideos.Tag, getVideos.PageSize);
            }
            else
            {
                preparedStatement = await _statementCache.NoContext.GetOrAddAsync("SELECT * FROM videos_by_tag WHERE tag = ? AND videoid >= ? LIMIT ?");
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
            PreparedStatement preparedStatement = await _statementCache.NoContext.GetOrAddAsync("SELECT tag FROM tags_by_letter WHERE first_letter = ? AND tag >= ? LIMIT ?");
            BoundStatement boundStatement = preparedStatement.Bind(firstLetter, getTags.TagStartsWith, getTags.PageSize);
            RowSet rows = await _session.ExecuteAsync(boundStatement);
            return new TagsStartingWith
            {
                TagStartsWith = getTags.TagStartsWith,
                Tags = rows.Select(row => row.GetValue<string>("tag")).ToList()
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