using System;

namespace KillrVideo.Models.Videos
{
    /// <summary>
    /// Model for indicating a new video was added successfully.
    /// </summary>
    [Serializable]
    public class NewVideoAddedViewModel
    {
        /// <summary>
        /// The URL where the video that was added can be viewed at.
        /// </summary>
        public string ViewVideoUrl { get; set; }
    }
}