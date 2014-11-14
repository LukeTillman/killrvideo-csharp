using System;
using Nimbus.MessageContracts;

namespace KillrVideo.Comments.Messages.Commands
{
    /// <summary>
    /// DTO for commenting on a video.
    /// </summary>
    [Serializable]
    public class CommentOnVideo : IBusCommand
    {
        public Guid VideoId { get; set; }
        public Guid UserId { get; set; }
        public string Comment { get; set; }
        public DateTimeOffset CommentTimestamp { get; set; }
    }
}
