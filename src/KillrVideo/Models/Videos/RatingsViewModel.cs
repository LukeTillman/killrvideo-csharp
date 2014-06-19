using System;

namespace KillrVideo.Models.Videos
{
    /// <summary>
    /// The view model for returning ratings data about a video.
    /// </summary>
    [Serializable]
    public class RatingsViewModel
    {
        public Guid VideoId { get; set; }
        public bool CurrentUserLoggedIn { get; set; }
        public int CurrentUserRating { get; set; }
        public long RatingsCount { get; set; }
        public long RatingsSum { get; set; }
    }
}