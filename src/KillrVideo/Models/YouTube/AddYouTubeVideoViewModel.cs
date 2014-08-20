using System;
using KillrVideo.Models.Shared;

namespace KillrVideo.Models.YouTube
{
    /// <summary>
    /// The request ViewModel for adding a new YouTube video.
    /// </summary>
    [Serializable]
    public class AddYouTubeVideoViewModel : IAddVideoViewModel
    {
        /// <summary>
        /// The name/title of the video.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The description for the video.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The Id for the YouTube video (can be found in the URL for the video as v=XXXXXX).
        /// </summary>
        public string YouTubeVideoId { get; set; }

        /// <summary>
        /// Any tags for the video, as a comma-delimited string.
        /// </summary>
        public string Tags { get; set; }
    }
}