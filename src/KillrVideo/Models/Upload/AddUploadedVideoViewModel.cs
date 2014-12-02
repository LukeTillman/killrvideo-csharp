using System;
using KillrVideo.Models.Shared;

namespace KillrVideo.Models.Upload
{
    /// <summary>
    /// ViewModel for requesting an uploaded video be added.
    /// </summary>
    [Serializable]
    public class AddUploadedVideoViewModel : IAddVideoViewModel
    {
        /// <summary>
        /// The URL where the video was uploaded.
        /// </summary>
        public string UploadUrl { get; set; }

        /// <summary>
        /// The name/title for the video.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The description for the video.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Any tags for the video.
        /// </summary>
        public string[] Tags { get; set; }
    }
}