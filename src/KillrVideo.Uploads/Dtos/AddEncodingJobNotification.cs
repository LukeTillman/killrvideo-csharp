using System;

namespace KillrVideo.Uploads.Dtos
{
    /// <summary>
    /// DTO for a notification about an Azure Media Services encoding job.
    /// </summary>
    [Serializable]
    public class AddEncodingJobNotification
    {
        public string JobId { get; set; }
        public DateTimeOffset StatusDate { get; set; }
        public string ETag { get; set; }
        public string NewState { get; set; }
        public string OldState { get; set; }
    }
}