using System;
using System.Collections.Generic;
using KillrVideo.Models.Shared;

namespace KillrVideo.Models.Videos
{
    /// <summary>
    /// The results for getting recommended videos.
    /// </summary>
    [Serializable]
    public class RecommendedVideosViewModel
    {
        public IEnumerable<VideoPreviewViewModel> Videos { get; set; }
        public string PagingState { get; set; }
    }
}