using System;
using System.Threading.Tasks;
using KillrVideo.Ratings.Dtos;

namespace KillrVideo.Ratings
{
    /// <summary>
    /// The public API for the video ratings service.
    /// </summary>
    public interface IRatingsService
    {
        /// <summary>
        /// Adds a user's rating of a video.
        /// </summary>
        Task RateVideo(RateVideo videoRating);

        /// <summary>
        /// Gets the current rating stats for the specified video.
        /// </summary>
        Task<VideoRating> GetRating(Guid videoId);

        /// <summary>
        /// Gets the rating given by a user for a specific video.  Will return 0 for the rating if the user hasn't rated the video.
        /// </summary>
        Task<UserVideoRating> GetRatingFromUser(Guid videoId, Guid userId);
    }
}
