using System;
using KillrVideo.Models.Shared;

namespace KillrVideo.Models.Search
{
    /// <summary>
    /// View model for searching videos.
    /// </summary>
    [Serializable]
    public class SearchVideosViewModel
    {
        /// <summary>
        /// The search query.
        /// </summary>
        public string Query { get; set; }

        /// <summary>
        /// The number of videos to return.
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// The first video on the page to retrieve (will be null the first time a page is requested).
        /// </summary>
        public VideoPreviewViewModel FirstVideoOnPage { get; set; }
    }
}