using System;

namespace KillrVideo.Models.Comments
{
    /// <summary>
    /// A single comment on a video.
    /// </summary>
    [Serializable]
    public class VideoCommentViewModel
    {
        public Guid CommentId { get; set; }

        public string UserProfileUrl { get; set; }
        public string UserFirstName { get; set; }
        public string UserLastName { get; set; }
        public string UserGravatarImageUrl { get; set; }

        public string Comment { get; set; }
        public DateTimeOffset CommentTimestamp { get; set; }
    }
}