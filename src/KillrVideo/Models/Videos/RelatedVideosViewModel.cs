using System;
using System.Collections.Generic;
using KillrVideo.Models.Shared;

namespace KillrVideo.Models.Videos
{
    /// <summary>
    /// The results for getting related videos.
    /// </summary>
    [Serializable]
    public class RelatedVideosViewModel
    {
        public Guid VideoId { get; set; }
        public IEnumerable<VideoPreviewViewModel> Videos { get; set; }
        public string PagingState { get; set; }
    }
}