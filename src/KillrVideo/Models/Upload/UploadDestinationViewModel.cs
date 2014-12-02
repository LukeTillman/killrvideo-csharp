using System;

namespace KillrVideo.Models.Upload
{
    /// <summary>
    /// Return model for a successfully created asset.
    /// </summary>
    [Serializable]
    public class UploadDestinationViewModel
    {
        /// <summary>
        /// The storage URL in Azure Media Services where the video can be uploaded.
        /// </summary>
        public string UploadUrl { get; set; }
    }
}