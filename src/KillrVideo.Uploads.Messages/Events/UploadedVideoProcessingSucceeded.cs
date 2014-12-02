using System;
using Nimbus.MessageContracts;

namespace KillrVideo.Uploads.Messages.Events
{
    /// <summary>
    /// Indicates video processing was a success on an uploaded video.
    /// </summary>
    [Serializable]
    public class UploadedVideoProcessingSucceeded : IBusEvent
    {
        public Guid VideoId { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public string VideoUrl { get; set; }
        public string ThumbnailUrl { get; set; }
    }
}