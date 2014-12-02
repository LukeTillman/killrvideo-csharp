using System;
using System.Collections.Generic;

namespace KillrVideo.Comments.ReadModel.Dtos
{
    /// <summary>
    /// Represents a page of comments for a video.
    /// </summary>
    [Serializable]
    public class VideoComments
    {
        public Guid VideoId { get; set; }
        public IEnumerable<VideoComment> Comments { get; set; } 
    }
}