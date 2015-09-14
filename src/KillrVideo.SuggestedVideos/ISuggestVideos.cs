using System.Threading.Tasks;
using KillrVideo.SuggestedVideos.Dtos;

namespace KillrVideo.SuggestedVideos
{
    /// <summary>
    /// The public API for the video suggestion service.
    /// </summary>
    public interface ISuggestVideos
    {
        /// <summary>
        /// Gets the videos related to the specified video.
        /// </summary>
        Task<RelatedVideos> GetRelatedVideos(RelatedVideosQuery query);

        /// <summary>
        /// Gets the personalized video suggestions for a specific user.
        /// </summary>
        Task<Dtos.SuggestedVideos> GetSuggestions(SuggestedVideosQuery query);
    }
}
