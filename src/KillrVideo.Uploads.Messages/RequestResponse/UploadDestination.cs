using System;
using Nimbus.MessageContracts;

namespace KillrVideo.Uploads.Messages.RequestResponse
{
    /// <summary>
    /// The destination for an upload.
    /// </summary>
    [Serializable]
    public class UploadDestination : IBusResponse
    {
        /// <summary>
        /// If there was a problem creating the upload destination, the error message will be here, otherwise null.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// The URL in Azure storage where the file can be directly uploaded.
        /// </summary>
        public string UploadUrl { get; set; }
    }
}