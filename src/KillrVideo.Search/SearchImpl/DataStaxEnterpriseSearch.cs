using System;
using System.Linq;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.Search.Dtos;
using KillrVideo.Utils;

namespace KillrVideo.Search.SearchImpl
{
    /// <summary>
    /// Searches videos using DataStax Enterprise search (Solr integration).
    /// </summary>
    public class DataStaxEnterpriseSearch : ISearchVideos
    {
        private readonly ISession _session;
        private readonly TaskCache<string, PreparedStatement> _statementCache;

        public DataStaxEnterpriseSearch(ISession session, TaskCache<string, PreparedStatement> statementCache)
        {
            if (session == null) throw new ArgumentNullException("session");
            if (statementCache == null) throw new ArgumentNullException("statementCache");
            _session = session;
            _statementCache = statementCache;
        }

        /// <summary>
        /// Gets a page of videos for a search query.
        /// </summary>
        public async Task<VideosForSearchQuery> SearchVideos(SearchVideosQuery searchVideosQuery)
        {
            // Do a Solr query against DSE search to find videos using Solr's ExtendedDisMax query parser. Query the
            // name, tags, and description fields in the videos table giving a boost to matches in the name and tags
            // fields as opposed to the description field
            // More info on ExtendedDisMax: http://wiki.apache.org/solr/ExtendedDisMax
            string solrQuery = "{ \"q\": \"{!edismax qf=\\\"name^2 tags^1 description\\\"}" + searchVideosQuery.Query + "\" }";
            
            PreparedStatement prepared = await _statementCache.NoContext.GetOrAddAsync(
                "SELECT videoid, userid, name, preview_image_location, added_date FROM videos WHERE solr_query=?");

            // The driver's built-in paging feature just works with DSE Search Solr paging which is pretty cool
            IStatement bound = prepared.Bind(solrQuery)
                                       .SetAutoPage(false)
                                       .SetPageSize(searchVideosQuery.PageSize);

            // The initial query won't have a paging state, but subsequent calls should if there are more pages
            if (string.IsNullOrEmpty(searchVideosQuery.PagingState) == false)
                bound.SetPagingState(Convert.FromBase64String(searchVideosQuery.PagingState));

            RowSet rows = await _session.ExecuteAsync(bound).ConfigureAwait(false);
            
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
        public Task<SuggestedQueries> GetQuerySuggestions(GetQuerySuggestions getSuggestions)
        {
            // TODO: Implement typeahead support
            return Task.FromResult(new SuggestedQueries
            {
                Query = getSuggestions.Query,
                Suggestions = Enumerable.Empty<string>()
            });
        }

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