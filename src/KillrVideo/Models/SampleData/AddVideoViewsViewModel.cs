using System;

namespace KillrVideo.Models.SampleData
{
    /// <summary>
    /// View model for adding sample views to videos on the site.
    /// </summary>
    [Serializable]
    public class AddVideoViewsViewModel
    {
        public int NumberOfViews { get; set; }
    }
}