using System;
using System.Collections.Generic;

namespace KillrVideo.Data.Videos.Dtos
{
    /// <summary>
    /// DTO representing the latest videos.
    /// </summary>
    [Serializable]
    public class LatestVideos
    {
        public IEnumerable<VideoPreview> Videos { get; set; }
    }
}