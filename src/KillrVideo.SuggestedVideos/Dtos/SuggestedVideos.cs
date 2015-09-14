using System;
using System.Collections.Generic;

namespace KillrVideo.SuggestedVideos.Dtos
{
    /// <summary>
    /// Videos suggested for a particular user.
    /// </summary>
    [Serializable]
    public class SuggestedVideos
    {
        public Guid UserId { get; set; }
        public IEnumerable<VideoPreview> Videos { get; set; }
        public string PagingState { get; set; }
    }
}
