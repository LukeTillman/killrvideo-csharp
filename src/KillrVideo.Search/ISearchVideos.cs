using System.Threading.Tasks;
using KillrVideo.Search.Dtos;

namespace KillrVideo.Search
{
    /// <summary>
    /// The public API for the search service for searching videos.
    /// </summary>
    public interface ISearchVideos
    {
        /// <summary>
        /// Gets a page of videos for a search query.
        /// </summary>
        Task<VideosForSearchQuery> SearchVideos(SearchVideosQuery searchVideosQuery);

        /// <summary>
        /// Gets a list of query suggestions for providing typeahead support.
        /// </summary>
        Task<SuggestedQueries> GetQuerySuggestions(GetQuerySuggestions getSuggestions);
    }
}
