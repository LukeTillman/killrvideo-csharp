using System.Threading.Tasks;
using KillrVideo.Search.Dtos;

namespace KillrVideo.Search
{
    /// <summary>
    /// Component for searching videos by tag.
    /// </summary>
    public interface ISearchVideosByTag
    {
        /// <summary>
        /// Gets a page of videos by tag.
        /// </summary>
        Task<VideosByTag> GetVideosByTag(GetVideosByTag getVideos);

        /// <summary>
        /// Gets a list of tags starting with specified text.
        /// </summary>
        Task<TagsStartingWith> GetTagsStartingWith(GetTagsStartingWith getTags);
    }
}
