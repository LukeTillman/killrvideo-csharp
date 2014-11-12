using System;
using System.Threading.Tasks;
using KillrVideo.SuggestedVideos.Dtos;

namespace KillrVideo.SuggestedVideos
{
    /// <summary>
    /// Suggests videos that might be interesting to a user.
    /// </summary>
    public interface ISuggestVideos
    {
        /// <summary>
        /// Gets the first 5 videos related to the specified video.
        /// </summary>
        Task<RelatedVideos> GetRelatedVideos(Guid videoId);
    }
}
