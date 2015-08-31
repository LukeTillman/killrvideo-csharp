using System;

namespace KillrVideo.SuggestedVideos.MLT
{
    /// <summary>
    /// ViewModel for recording video playback starting.
    /// </summary>
    [Serializable]
    public class MLTDocument
    {
        public Guid videoid { get; set; }
        public string name { get; set; }
        public DateTimeOffset added_date { get; set; }
        public string preview_image_location { get; set; }
        public string description { get; set; }
        public string location { get; set; }
        public Guid userid { get; set; }
        public int location_type { get; set; }
    }
}