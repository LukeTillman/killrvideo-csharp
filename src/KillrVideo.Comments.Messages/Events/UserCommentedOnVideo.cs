using System;
using Nimbus.MessageContracts;

namespace KillrVideo.Comments.Messages.Events
{
    /// <summary>
    /// Event for when a user posts a comment on a video.
    /// </summary>
    public class UserCommentedOnVideo : IBusEvent
    {
        public Guid UserId { get; set; }
        public Guid VideoId { get; set; }
        public Guid CommentId { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }
}
