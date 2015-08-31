using System;

namespace KillrVideo.SuggestedVideos.MLT

{
    /// <summary>
    /// ViewModel for recording video playback starting.
    /// </summary>
    [Serializable]
    public class MLTResponseHeader
    {
        public int status { get; set; }
        public int QTime { get; set; }
    }
}