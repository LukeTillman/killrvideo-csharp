using System;
using System.Collections.Generic;

namespace KillrVideo.SampleData.Components.YouTube
{
    /// <summary>
    /// Represents a video returned from YouTube.
    /// </summary>
    [Serializable]
    public class YouTubeVideo
    {
        public YouTubeVideoSource Source { get; set; }
        public string YouTubeVideoId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTimeOffset PublishedAt { get; set; }
        public HashSet<string> SuggestedTags { get; set; } 
    }
}