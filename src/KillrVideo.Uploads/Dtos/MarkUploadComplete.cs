using System;
using Nimbus.MessageContracts;

namespace KillrVideo.Uploads.Dtos
{
    /// <summary>
    /// Marks an upload as complete.
    /// </summary>
    [Serializable]
    public class MarkUploadComplete : IBusCommand
    {
        public string UploadUrl { get; set; }
    }
}
