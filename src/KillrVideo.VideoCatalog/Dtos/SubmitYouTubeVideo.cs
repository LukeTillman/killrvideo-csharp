using System;
using System.Collections.Generic;

namespace KillrVideo.VideoCatalog.Dtos
{
    /// <summary>
    /// Submits a YouTube video to the catalog.
    /// </summary>
    [Serializable]
    public class SubmitYouTubeVideo
    {
        public Guid VideoId { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public HashSet<string> Tags { get; set; }
        public string YouTubeVideoId { get; set; }
    }
}