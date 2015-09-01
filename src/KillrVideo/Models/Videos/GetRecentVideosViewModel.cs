using System;

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
        /// The current page state. Will be null on initial page request, then may have a value on subsequent requests.
        /// </summary>
        public string PagingState { get; set; }
    }
}