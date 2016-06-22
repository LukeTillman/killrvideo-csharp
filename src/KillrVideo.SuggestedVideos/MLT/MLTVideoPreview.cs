using System;

namespace KillrVideo.SuggestedVideos.MLT
{ 
    /// <summary>
    /// DTO representing some basic details of a video necessary for showing a preview.
    /// </summary>
    [Serializable]
    public class MLTVideoPreview
    {
        public Guid VideoId { get; set; }
        public DateTimeOffset AddedDate { get; set; }
        public string Name { get; set; }
        public string PreviewImageLocation { get; set; }
        public Guid UserId { get; set; }
    }
}