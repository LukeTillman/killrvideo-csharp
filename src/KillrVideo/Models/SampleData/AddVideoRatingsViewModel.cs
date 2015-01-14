using System;

namespace KillrVideo.Models.SampleData
{
    /// <summary>
    /// View model for adding sample ratings to videos on the site.
    /// </summary>
    [Serializable]
    public class AddVideoRatingsViewModel
    {
        public int NumberOfRatings { get; set; }
    }
}