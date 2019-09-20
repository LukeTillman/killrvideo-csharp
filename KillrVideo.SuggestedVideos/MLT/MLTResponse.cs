using System;
using System.Collections.Generic;

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
        public List<MLTVideoPreview> Docs { get; set; }
    }
}