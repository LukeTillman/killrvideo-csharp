using System;
using Nimbus.MessageContracts;

namespace KillrVideo.Uploads.Dtos
{
    /// <summary>
    /// Request for generating a destination for an upload.
    /// </summary>
    [Serializable]
    public class GenerateUploadDestination : IBusRequest<GenerateUploadDestination, UploadDestination>
    {
        /// <summary>
        /// The file name to be uploaded.
        /// </summary>
        public string FileName { get; set; }
    }
}
