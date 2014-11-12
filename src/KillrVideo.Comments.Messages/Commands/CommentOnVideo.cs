using System;

namespace KillrVideo.Comments.Api.Commands
{
    /// <summary>
    /// DTO for commenting on a video.
    /// </summary>
    [Serializable]
    public class CommentOnVideo
    {
        public Guid VideoId { get; set; }
        public Guid UserId { get; set; }
        public string Comment { get; set; }
        public DateTimeOffset CommentTimestamp { get; set; }
    }
}
