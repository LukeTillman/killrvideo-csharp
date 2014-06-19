using System;

namespace KillrVideo.Models.Videos
{
    /// <summary>
    /// The view model for requesting ratings data for a video.
    /// </summary>
    [Serializable]
    public class GetRatingsViewModel
    {
        public Guid VideoId { get; set; }
    }
}