namespace KillrVideo.Data.Upload
{
    /// <summary>
    /// Constants related to video upload.
    /// </summary>
    public static class UploadConfigConstants
    {
        /// <summary>
        /// The queue name used for progress/completion notifications about Azure Media Services encoding jobs.
        /// </summary>
        public const string NotificationQueueName = "video-job-notifications";
    }
}
