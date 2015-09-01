using System;

namespace KillrVideo.SuggestedVideos.MLT
{
    /// <summary>
    /// The results from issuing a MoreLikeThis query to DSE Search.
    /// </summary>
    [Serializable]
    public class MLTQueryResult
    {
        public MLTResponse Response { get; set; }
    }
}