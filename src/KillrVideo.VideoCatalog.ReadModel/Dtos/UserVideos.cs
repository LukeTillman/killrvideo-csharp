using System;
using System.Collections.Generic;

namespace KillrVideo.VideoCatalog.ReadModel.Dtos
{
    /// <summary>
    /// Represents a page of videos for a user.
    /// </summary>
    [Serializable]
    public class UserVideos
    {
        public Guid UserId { get; set; }
        public IEnumerable<VideoPreview> Videos { get; set; }
    }
}