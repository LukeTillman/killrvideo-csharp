using System;

namespace KillrVideo.Models.Comments
{
    /// <summary>
    /// A single comment by a user.
    /// </summary>
    [Serializable]
    public class UserCommentViewModel
    {
        public Guid CommentId { get; set; }
        
        public Guid VideoId { get; set; }
        public string VideoName { get; set; }
        public string VideoPreviewImageLocation { get; set; }
        
        public string Comment { get; set; }
        public DateTimeOffset CommentTimestamp { get; set; }
    }
}