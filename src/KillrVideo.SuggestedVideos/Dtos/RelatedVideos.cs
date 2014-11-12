using System;
using System.Collections.Generic;

namespace KillrVideo.SuggestedVideos.Dtos
{
    /// <summary>
    /// Represents videos related to another video.
    /// </summary>
    [Serializable]
    public class RelatedVideos
    {
        public Guid VideoId { get; set; }
        public IEnumerable<VideoPreview> Videos { get; set; }
    }
}