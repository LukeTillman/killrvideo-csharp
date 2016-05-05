using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using Cassandra;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using KillrVideo.Cassandra;
using KillrVideo.Host.Config;
using KillrVideo.Protobuf;
using KillrVideo.Protobuf.Services;

namespace KillrVideo.Search
{
    /// <summary>
    /// Searches for videos by tag in Cassandra.
    /// </summary>
    [Export(typeof(IGrpcServerService))]
    public class SearchVideosByTag : SearchService.ISearchService, IConditionalGrpcServerService
    {
        private readonly ISession _session;
        private readonly PreparedStatementCache _statementCache;

        public SearchVideosByTag(ISession session, PreparedStatementCache statementCache)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (statementCache == null) throw new ArgumentNullException(nameof(statementCache));
            _session = session;
            _statementCache = statementCache;
        }

        /// <summary>
        /// Convert this instance to a ServerServiceDefinition that can be run on a Grpc server.
        /// </summary>
        public ServerServiceDefinition ToServerServiceDefinition()
        {
            return SearchService.BindService(this);
        }

        /// <summary>
        /// Returns true if this service should run given the configuration of the host.
        /// </summary>
        public bool ShouldRun(IHostConfiguration hostConfig)
        {
            // Use this implementation when DSE Search is not enabled or not present in the host config
            return SearchConfig.UseDseSearch(hostConfig) == false;
        }

        /// <summary>
        /// Gets a page of videos for a search query (looks for videos with that tag).
        /// </summary>
        public async Task<SearchVideosResponse> SearchVideos(SearchVideosRequest request, ServerCallContext context)
        {
            // Use the driver's built-in paging feature to get only a page of rows
            PreparedStatement preparedStatement = await _statementCache.GetOrAddAsync("SELECT * FROM videos_by_tag WHERE tag = ?");
            IStatement boundStatement = preparedStatement.Bind(request.Query)
                                                         .SetAutoPage(false)
                                                         .SetPageSize(request.PageSize);

            // The initial query won't have a paging state, but subsequent calls should if there are more pages
            if (string.IsNullOrEmpty(request.PagingState) == false)
                boundStatement.SetPagingState(Convert.FromBase64String(request.PagingState));

            RowSet rows = await _session.ExecuteAsync(boundStatement).ConfigureAwait(false);
            var response = new SearchVideosResponse
            {
                Query = request.Query,
                PagingState = rows.PagingState != null && rows.PagingState.Length > 0 ? Convert.ToBase64String(rows.PagingState) : ""
            };

            response.Videos.Add(rows.Select(MapRowToVideoPreview));
            return response;
        }

        /// <summary>
        /// Gets a list of query suggestions for providing typeahead support.
        /// </summary>
        public async Task<GetQuerySuggestionsResponse> GetQuerySuggestions(GetQuerySuggestionsRequest request, ServerCallContext context)
        {
            string firstLetter = request.Query.Substring(0, 1);
            PreparedStatement preparedStatement = await _statementCache.GetOrAddAsync("SELECT tag FROM tags_by_letter WHERE first_letter = ? AND tag >= ? LIMIT ?");
            BoundStatement boundStatement = preparedStatement.Bind(firstLetter, request.Query, request.PageSize);
            RowSet rows = await _session.ExecuteAsync(boundStatement).ConfigureAwait(false);
            var response = new GetQuerySuggestionsResponse
            {
                Query = request.Query
            };
            response.Suggestions.Add(rows.Select(row => row.GetValue<string>("tag")));
            return response;
        }

        /// <summary>
        /// Maps a row to a VideoPreview object.
        /// </summary>
        private static SearchResultsVideoPreview MapRowToVideoPreview(Row row)
        {
            return new SearchResultsVideoPreview
            {
                VideoId = row.GetValue<Guid>("videoid").ToUuid(),
                AddedDate = row.GetValue<DateTimeOffset>("added_date").ToTimestamp(),
                Name = row.GetValue<string>("name"),
                PreviewImageLocation = row.GetValue<string>("preview_image_location"),
                UserId = row.GetValue<Guid>("userid").ToUuid()
            };
        }
    }
}