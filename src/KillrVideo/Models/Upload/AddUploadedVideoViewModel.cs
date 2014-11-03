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
        /// The asset's Id.
        /// </summary>
        public string AssetId { get; set; }

        /// <summary>
        /// The file name.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// The unique Id for the upload locator.
        /// </summary>
        public string UploadLocatorId { get; set; }

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