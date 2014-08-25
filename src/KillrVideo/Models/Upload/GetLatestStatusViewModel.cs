using System;

namespace KillrVideo.Models.Upload
{
    /// <summary>
    /// Request for getting the latest status on an uploaded video.
    /// </summary>
    [Serializable]
    public class GetLatestStatusViewModel
    {
        /// <summary>
        /// The job Id to get the latest status for.
        /// </summary>
        public string JobId { get; set; }
    }
}