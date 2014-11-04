using System;
using KillrVideo.Models.Shared;

namespace KillrVideo.Models.Videos
{
    /// <summary>
    /// Request model for getting recent videos.
    /// </summary>
    [Serializable]
    public class GetRecentVideosViewModel
    {
        /// <summary>
        /// The number of latest videos to get.
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// The first video on the page of records to show.
        /// </summary>
        public VideoPreviewViewModel FirstVideoOnPage { get; set; }
    }
}