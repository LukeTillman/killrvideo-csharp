using System;
using Nimbus.MessageContracts;

namespace KillrVideo.Uploads.Messages.Events
{
    /// <summary>
    /// Indicates video processing failed on an uploaded video.
    /// </summary>
    [Serializable]
    public class UploadedVideoProcessingFailed : IBusEvent
    {
        public Guid VideoId { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }
}