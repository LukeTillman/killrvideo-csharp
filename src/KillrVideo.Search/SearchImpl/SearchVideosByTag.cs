using System;
using System.Linq;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.Search.Dtos;
using KillrVideo.Utils;

namespace KillrVideo.Search.SearchImpl
{
    /// <summary>
    /// Searches for videos by tag in Cassandra.
    /// </summary>
    public class SearchVideosByTag : ISearchVideos
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
        /// Gets a page of videos for a search query (looks for videos with that tag).
        /// </summary>
        public async Task<VideosForSearchQuery> SearchVideos(SearchVideosQuery searchVideosQuery)
        {
            // Use the driver's built-in paging feature to get only a page of rows
            PreparedStatement preparedStatement = await _statementCache.NoContext.GetOrAddAsync("SELECT * FROM videos_by_tag WHERE tag = ?");
            IStatement boundStatement = preparedStatement.Bind(searchVideosQuery.Query)
                                                         .SetAutoPage(false)
                                                         .SetPageSize(searchVideosQuery.PageSize);

            // The initial query won't have a paging state, but subsequent calls should if there are more pages
            if (string.IsNullOrEmpty(searchVideosQuery.PagingState) == false)
                boundStatement.SetPagingState(Convert.FromBase64String(searchVideosQuery.PagingState));

            RowSet rows = await _session.ExecuteAsync(boundStatement).ConfigureAwait(false);
            return new VideosForSearchQuery
            {
                Query = searchVideosQuery.Query,
                Videos = rows.Select(MapRowToVideoPreview).ToList(),
                PagingState = rows.PagingState != null && rows.PagingState.Length > 0 ? Convert.ToBase64String(rows.PagingState) : null
            };
        }

        /// <summary>
        /// Gets a list of query suggestions for providing typeahead support.
        /// </summary>
        public async Task<SuggestedQueries> GetQuerySuggestions(GetQuerySuggestions getSuggestions)
        {
            string firstLetter = getSuggestions.Query.Substring(0, 1);
            PreparedStatement preparedStatement = await _statementCache.NoContext.GetOrAddAsync("SELECT tag FROM tags_by_letter WHERE first_letter = ? AND tag >= ? LIMIT ?");
            BoundStatement boundStatement = preparedStatement.Bind(firstLetter, getSuggestions.Query, getSuggestions.PageSize);
            RowSet rows = await _session.ExecuteAsync(boundStatement).ConfigureAwait(false);
            return new SuggestedQueries
            {
                Query = getSuggestions.Query,
                Suggestions = rows.Select(row => row.GetValue<string>("tag")).ToList()
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