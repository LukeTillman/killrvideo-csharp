using System;

namespace KillrVideo.Comments.Dtos
{
    /// <summary>
    /// A comment by a user.
    /// </summary>
    [Serializable]
    public class UserComment
    {
        public Guid CommentId { get; set; }
        public Guid VideoId { get; set; }
        public string Comment { get; set; }
        public DateTimeOffset CommentTimestamp { get; set; }
    }
}