using System;
using Nimbus.MessageContracts;

namespace KillrVideo.Uploads.Messages.Events
{
    /// <summary>
    /// Event for when an uploaded video has been published and is ready for playback.
    /// </summary>
    [Serializable]
    public class UploadedVideoPublished : IBusEvent
    {
        public Guid VideoId { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public string VideoUrl { get; set; }
        public string ThumbnailUrl { get; set; }
    }
}
