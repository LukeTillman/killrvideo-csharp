using System;
using System.Collections.Generic;
namespace KillrVideo.SuggestedVideos.MLT
{
    /// <summary>
    /// ViewModel for recording video playback starting.
    /// </summary>
    [Serializable]
    public class MLTMatch
    {
        public int numFound { get; set; }
        public int start { get; set; }
        public IEnumerable<MLTDocument> docs { get; set; }
    }
}