using System;
using Nimbus.MessageContracts;

namespace KillrVideo.Uploads.Worker.InternalEvents
{
    /// <summary>
    /// Event for when a video file is finished uploading to Azure Media Services.
    /// </summary>
    [Serializable]
    public class UploadCompleted : IBusEvent
    {
        public string AssetId { get; set; }
        public string Filename { get; set; }
    }
}
