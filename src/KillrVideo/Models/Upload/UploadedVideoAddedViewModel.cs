using System;

namespace KillrVideo.Models.Upload
{
    /// <summary>
    /// Response data for an uploaded video being added.
    /// </summary>
    [Serializable]
    public class UploadedVideoAddedViewModel
    {
        /// <summary>
        /// A URL where the uploaded video can be viewed.
        /// </summary>
        public string ViewVideoUrl { get; set; }
    }
}