using System;
using System.Collections.Generic;

namespace KillrVideo.VideoCatalog.Dtos
{
    /// <summary>
    /// Submits an uploaded video to the catalog.
    /// </summary>
    [Serializable]
    public class SubmitUploadedVideo
    {
        public Guid VideoId { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public HashSet<string> Tags { get; set; }
        public string UploadUrl { get; set; }
    }
}