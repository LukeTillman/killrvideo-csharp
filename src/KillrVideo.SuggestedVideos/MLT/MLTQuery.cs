using System;

namespace KillrVideo.SuggestedVideos.MLT
{
    /// <summary>
    /// The response from issuing a MoreLikeThis query to DSE Search.
    /// </summary>
    [Serializable]
    public class MLTQuery
    {
        public MLTResponseHeader responseHeader { get; set; }
        public MLTMatch match { get; set; }
        public MLTResponse response { get; set; }
    }
}