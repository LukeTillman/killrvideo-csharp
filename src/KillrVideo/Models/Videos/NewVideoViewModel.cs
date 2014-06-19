using System;

namespace KillrVideo.Models.Videos
{
    /// <summary>
    /// Model for adding a new video.
    /// </summary>
    [Serializable]
    public class NewVideoViewModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public string LocationType { get; set; }
        public string Tags { get; set; }
    }
}