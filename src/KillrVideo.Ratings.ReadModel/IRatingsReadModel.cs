using System;
using System.Threading.Tasks;
using KillrVideo.Ratings.ReadModel.Dtos;

namespace KillrVideo.Ratings.ReadModel
{
    /// <summary>
    /// Handles reads for user ratings data of videos.
    /// </summary>
    public interface IRatingsReadModel
    {
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