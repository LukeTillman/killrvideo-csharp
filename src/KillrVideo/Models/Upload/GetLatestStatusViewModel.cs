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
        /// The video Id to get the latest status for.
        /// </summary>
        public Guid VideoId { get; set; }
    }
}