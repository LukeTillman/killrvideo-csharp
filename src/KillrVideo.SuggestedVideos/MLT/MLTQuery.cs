using System;

namespace KillrVideo.SuggestedVideos.MLT
{
    /// <summary>
    /// ViewModel for recording video playback starting.
    /// </summary>
    [Serializable]
    public class MLTQuery
    {
        public MLTResponseHeader responseHeader { get; set; }
        public MLTMatch match { get; set; }
        public MLTResponse response { get; set; }
    }
}