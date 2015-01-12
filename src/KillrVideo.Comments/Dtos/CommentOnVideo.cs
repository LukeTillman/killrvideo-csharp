using System;
using Cassandra;

namespace KillrVideo.Comments.Dtos
{
    /// <summary>
    /// DTO for commenting on a video.
    /// </summary>
    [Serializable]
    public class CommentOnVideo
    {
        public Guid VideoId { get; set; }
        public Guid UserId { get; set; }
        public TimeUuid CommentId { get; set; }
        public string Comment { get; set; }
    }
}
