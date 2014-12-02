using System;
using System.Collections.Generic;

namespace KillrVideo.VideoCatalog.Messages.Events
{
    /// <summary>
    /// Event for when a new uploaded video is added to the catalog and ready for playback.
    /// </summary>
    [Serializable]
    public class UploadedVideoAdded : IVideoAdded
    {
        public Guid VideoId { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public string PreviewImageLocation { get; set; }
        public ISet<string> Tags { get; set; }
        public DateTimeOffset Timestamp { get; set; }

        public UploadedVideoAdded()
        {
            Tags = new HashSet<string>();
        }
    }
}