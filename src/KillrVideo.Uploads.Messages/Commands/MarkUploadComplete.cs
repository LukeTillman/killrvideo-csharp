using System;
using Nimbus.MessageContracts;

namespace KillrVideo.Uploads.Messages.Commands
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
