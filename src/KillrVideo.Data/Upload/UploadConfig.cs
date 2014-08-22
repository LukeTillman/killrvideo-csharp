namespace KillrVideo.Data.Upload
{
    /// <summary>
    /// Constants related to video upload.
    /// </summary>
    public static class UploadConfig
    {
        /// <summary>
        /// The queue name used for progress/completion notifications about Azure Media Services encoding jobs.
        /// </summary>
        public const string NotificationQueueName = "video-job-notifications";

        /// <summary>
        /// A prefix to use for the asset name containing the encoded video.
        /// </summary>
        public const string EncodedVideoAssetNamePrefix = "Encoded - ";

        /// <summary>
        /// A prefix to use for the asset name containing the thumbnails. 
        /// </summary>
        public const string ThumbnailAssetNamePrefix = "Thumbnails - ";
    }
}
