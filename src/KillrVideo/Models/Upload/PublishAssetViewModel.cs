using System;

namespace KillrVideo.Models.Upload
{
    /// <summary>
    /// ViewModel for requesting an asset be published.
    /// </summary>
    [Serializable]
    public class PublishAssetViewModel
    {
        /// <summary>
        /// The asset's Id.
        /// </summary>
        public string AssetId { get; set; }

        /// <summary>
        /// The file name.
        /// </summary>
        public string FileName { get; set; }
    }
}