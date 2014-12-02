using System;
using Nimbus.MessageContracts;

namespace KillrVideo.VideoCatalog.Messages.Events
{
    /// <summary>
    /// Event for when an uploaded video is accepted by the video catalog, but isn't ready for playback yet.
    /// </summary>
    [Serializable]
    public class UploadedVideoAccepted : IBusEvent
    {
        public Guid VideoId { get; set; }
        public string UploadUrl { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }
}
