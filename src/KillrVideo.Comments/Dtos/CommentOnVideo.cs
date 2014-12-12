using System;
using Nimbus.MessageContracts;

namespace KillrVideo.Comments.Dtos
{
    /// <summary>
    /// DTO for commenting on a video.
    /// </summary>
    [Serializable]
    public class CommentOnVideo : IBusCommand
    {
        public Guid VideoId { get; set; }
        public Guid UserId { get; set; }
        public Guid CommentId { get; set; }
        public string Comment { get; set; }
    }
}
