using System;

namespace KillrVideo.VideoCatalog
{
    /// <summary>
    /// Some constants used in the video catalog service.
    /// </summary>
    public static class VideoCatalogConstants
    {
        // Video types for writes (should match Enum in ReadModel)
        public const int YouTubeVideoType = 0;
        public const int UploadedVideoType = 1;

        /// <summary>
        /// The TTL in seconds for latest videos records.
        /// </summary>
        public static readonly int LatestVideosTtlSeconds = Convert.ToInt32(TimeSpan.FromDays(7).TotalSeconds);
    }
}
