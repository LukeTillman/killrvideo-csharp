using System;

namespace KillrVideo.Ratings.Api.Commands
{
    /// <summary>
    /// DTO for rating a video.
    /// </summary>
    [Serializable]
    public class RateVideo
    {
        public Guid VideoId { get; set; }
        public Guid UserId { get; set; }
        public int Rating { get; set; }
    }
}
