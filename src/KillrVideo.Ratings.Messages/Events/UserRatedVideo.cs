using System;
using Nimbus.MessageContracts;

namespace KillrVideo.Ratings.Messages.Events
{
    /// <summary>
    /// Event for when a user rates a video.
    /// </summary>
    [Serializable]
    public class UserRatedVideo : IBusEvent
    {
        public Guid VideoId { get; set; }
        public Guid UserId { get; set; }
        public int Rating { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }
}
