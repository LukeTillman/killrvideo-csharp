using System;

namespace KillrVideo.Models.Upload
{
    /// <summary>
    /// Response with the latest status for an upload job.
    /// </summary>
    public class LatestStatusViewModel
    {
        public DateTimeOffset StatusDate { get; set; }
        public string Status { get; set; }
    }
}