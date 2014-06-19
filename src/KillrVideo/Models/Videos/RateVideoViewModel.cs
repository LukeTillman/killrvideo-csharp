using System;

namespace KillrVideo.Models.Videos
{
    /// <summary>
    /// The view model for submitting a rating for a video.
    /// </summary>
    [Serializable]
    public class RateVideoViewModel
    {
        public Guid VideoId { get; set; }
        public int Rating { get; set; }
    }
}