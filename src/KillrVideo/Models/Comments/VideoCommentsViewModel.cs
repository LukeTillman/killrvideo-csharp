using System;
using System.Collections.Generic;

namespace KillrVideo.Models.Comments
{
    /// <summary>
    /// Results of getting a page of comments for a video.
    /// </summary>
    [Serializable]
    public class VideoCommentsViewModel
    {
        public Guid VideoId { get; set; }
        public IEnumerable<VideoCommentViewModel> Comments { get; set; } 
    }
}