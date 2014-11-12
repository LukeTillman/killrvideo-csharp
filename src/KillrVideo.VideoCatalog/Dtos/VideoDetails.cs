using System;
using System.Collections.Generic;
using KillrVideo.VideoCatalog.Api;

namespace KillrVideo.VideoCatalog.Dtos
{
    /// <summary>
    /// DTO representing all the available details of a specific video.
    /// </summary>
    [Serializable]
    public class VideoDetails
    {
        public Guid VideoId { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public VideoLocationType LocationType { get; set; }
        public ISet<string> Tags { get; set; }
        public DateTimeOffset AddedDate { get; set; }
    }
}