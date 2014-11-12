using System;

namespace KillrVideo.Uploads.Dtos
{
    /// <summary>
    /// DTO that represents the encoding job progress for an uploaded video.
    /// </summary>
    [Serializable]
    public class EncodingJobProgress
    {
        public DateTimeOffset StatusDate { get; set; }
        public string CurrentState { get; set; }
    }
}