using System;
using System.Collections.Generic;
using KillrVideo.SuggestedVideos.Dtos;

namespace KillrVideo.SuggestedVideos.MLT
{    
    /// <summary>
    /// The response content of a MLTQueryResult. Contains the actual videos found.
    /// </summary>
    [Serializable]
    public class MLTResponse
    {
        public int NumFound { get; set; }
        public int Start { get; set; }
        public List<VideoPreview> Docs { get; set; }
    }
}