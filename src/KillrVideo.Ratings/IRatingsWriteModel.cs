using System.Threading.Tasks;
using KillrVideo.Ratings.Api.Commands;

namespace KillrVideo.Ratings
{
    /// <summary>
    /// Handles writes for user ratings of videos.
    /// </summary>
    public interface IRatingsWriteModel
    {
        /// <summary>
        /// Adds a rating for a video.
        /// </summary>
        Task RateVideo(RateVideo videoRating);
    }
}