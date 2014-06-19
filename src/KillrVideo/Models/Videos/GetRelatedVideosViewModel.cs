using System;

namespace KillrVideo.Models.Videos
{
    /// <summary>
    /// Request for getting related videos.
    /// </summary>
    [Serializable]
    public class GetRelatedVideosViewModel
    {
        /// <summary>
        /// The video Id to get related videos for.
        /// </summary>
        public Guid VideoId { get; set; }
    }
}