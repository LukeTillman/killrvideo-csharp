using System;
using Nimbus.MessageContracts;

namespace KillrVideo.Uploads.Messages.Events
{
    /// <summary>
    /// Indicates video processing has started on an uploaded video.
    /// </summary>
    [Serializable]
    public class UploadedVideoProcessingStarted : IBusEvent
    {
        public Guid VideoId { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }
}
