using System;
using Nimbus.MessageContracts;

namespace KillrVideo.Ratings.Dtos
{
    /// <summary>
    /// DTO for rating a video.
    /// </summary>
    [Serializable]
    public class RateVideo : IBusCommand
    {
        public Guid VideoId { get; set; }
        public Guid UserId { get; set; }
        public int Rating { get; set; }
    }
}
