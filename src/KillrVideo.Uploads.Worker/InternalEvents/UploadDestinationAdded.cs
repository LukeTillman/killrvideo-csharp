using System;
using Nimbus.MessageContracts;

namespace KillrVideo.Uploads.Worker.InternalEvents
{
    /// <summary>
    /// Internal event for when an upload destination is added to Azure Media Services.
    /// </summary>
    [Serializable]
    public class UploadDestinationAdded : IBusEvent
    {
        public string UploadUrl { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }
}
