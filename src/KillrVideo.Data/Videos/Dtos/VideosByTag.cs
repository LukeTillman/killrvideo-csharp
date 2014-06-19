using System;
using System.Collections.Generic;

namespace KillrVideo.Data.Videos.Dtos
{
    /// <summary>
    /// Represents a page of videos by tag.
    /// </summary>
    [Serializable]
    public class VideosByTag
    {
        public string Tag { get; set; }
        public IEnumerable<VideoPreview> Videos { get; set; }
    }
}
