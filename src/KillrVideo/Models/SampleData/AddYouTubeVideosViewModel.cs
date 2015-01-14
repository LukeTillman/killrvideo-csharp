using System;

namespace KillrVideo.Models.SampleData
{
    /// <summary>
    /// View model for adding sample YouTube videos to the site.
    /// </summary>
    [Serializable]
    public class AddYouTubeVideosViewModel
    {
        public int NumberOfVideos { get; set; }
    }
}