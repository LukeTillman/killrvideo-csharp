using System;

namespace KillrVideo.Models.YouTube
{
    /// <summary>
    /// The response ViewModel for when a new YouTube video has been added.
    /// </summary>
    [Serializable]
    public class YouTubeVideoAddedViewModel
    {
        /// <summary>
        /// The URL where the video can be viewed.
        /// </summary>
        public string ViewVideoUrl { get; set; }
    }
}