using System;
using System.Collections.Generic;
using KillrVideo.Models.Shared;

namespace KillrVideo.Models.Videos
{
    /// <summary>
    /// The results of getting a page of recent videos.
    /// </summary>
    [Serializable]
    public class RecentVideosViewModel
    {
        public IEnumerable<VideoPreviewViewModel> Videos { get; set; }
        public string PagingState { get; set; }
    }
}