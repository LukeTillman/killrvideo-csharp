using System;
using System.Collections.Generic;

namespace KillrVideo.Models.Videos
{
    [Serializable]
    public class RecentVideosViewModel
    {
        public IEnumerable<VideoPreviewViewModel> Videos { get; set; } 
    }
}