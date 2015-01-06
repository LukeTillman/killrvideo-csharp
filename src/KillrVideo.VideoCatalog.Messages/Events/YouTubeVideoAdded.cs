using System;
using System.Collections.Generic;

namespace KillrVideo.VideoCatalog.Messages.Events
{
    /// <summary>
    /// Event for when a YouTube video is added to the video catalog and ready for playback.
    /// </summary>
    [Serializable]
    public class YouTubeVideoAdded : IVideoAdded
    {
        public Guid VideoId { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public string PreviewImageLocation { get; set; }
        public HashSet<string> Tags { get; set; }
        public DateTimeOffset Timestamp { get; set; }

        public YouTubeVideoAdded()
        {
            Tags = new HashSet<string>();
        }
    }
}