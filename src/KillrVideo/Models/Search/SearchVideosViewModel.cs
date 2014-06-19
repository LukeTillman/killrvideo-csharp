using System;
using KillrVideo.Models.Videos;

namespace KillrVideo.Models.Search
{
    /// <summary>
    /// View model for searching videos by tag.
    /// </summary>
    [Serializable]
    public class SearchVideosViewModel
    {
        /// <summary>
        /// The tag to search for.
        /// </summary>
        public string Tag { get; set; }

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