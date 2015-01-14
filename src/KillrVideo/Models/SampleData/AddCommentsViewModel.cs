using System;

namespace KillrVideo.Models.SampleData
{
    /// <summary>
    /// View model for adding sample comments to videos on the site.
    /// </summary>
    [Serializable]
    public class AddCommentsViewModel
    {
        public int NumberOfComments { get; set; }
    }
}