using System;

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
        /// The paging state (used to retrieve subsequent pages of videos after the initial page).
        /// </summary>
        public string PagingState { get; set; }
    }
}