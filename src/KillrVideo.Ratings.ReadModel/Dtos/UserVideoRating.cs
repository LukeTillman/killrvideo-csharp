using System;

namespace KillrVideo.Ratings.ReadModel.Dtos
{
    /// <summary>
    /// Represents the rating given by a user for a specific video.
    /// </summary>
    [Serializable]
    public class UserVideoRating
    {
        public Guid VideoId { get; set; }
        public Guid UserId { get; set; }
        public int Rating { get; set; }
    }
}