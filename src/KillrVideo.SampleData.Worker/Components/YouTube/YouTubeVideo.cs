using System;

namespace KillrVideo.SampleData.Worker.Components.YouTube
{
    /// <summary>
    /// Represents a video returned from YouTube.
    /// </summary>
    public class YouTubeVideo
    {
        public YouTubeVideoSource Source { get; set; }
        public string YouTubeVideoId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTimeOffset PublishedAt { get; set; }
    }
}