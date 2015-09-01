using System;
using System.Collections.Generic;

namespace KillrVideo.VideoCatalog.Dtos
{
    /// <summary>
    /// DTO representing the latest videos.
    /// </summary>
    [Serializable]
    public class LatestVideos
    {
        public IEnumerable<VideoPreview> Videos { get; set; }
        public string PagingState { get; set; }
    }
}