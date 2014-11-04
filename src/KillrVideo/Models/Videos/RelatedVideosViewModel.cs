using System;
using System.Collections.Generic;
using KillrVideo.Models.Shared;

namespace KillrVideo.Models.Videos
{
    [Serializable]
    public class RelatedVideosViewModel
    {
        public IEnumerable<VideoPreviewViewModel> Videos { get; set; }
    }
}