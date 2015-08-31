using System;
using System.Collections.Generic;
using KillrVideo.SuggestedVideos.Dtos;
namespace KillrVideo.SuggestedVideos.MLT

{
    /// <summary>
    /// ViewModel for recording video playback starting.
    /// </summary>
    [Serializable]
    public class MLTResponse
    {
        public int numFound { get; set; }
        public int start { get; set; }
        public IEnumerable<VideoPreview> docs { get; set; }
    }
}