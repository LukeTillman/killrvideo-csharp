using System;
using System.Collections.Generic;
using KillrVideo.Models.Shared;

namespace KillrVideo.Models.Search
{
    /// <summary>
    /// Results of searching videos by tag.
    /// </summary>
    [Serializable]
    public class SearchResultsViewModel
    {
        /// <summary>
        /// The query that was searched.
        /// </summary>
        public string Query { get; set; }

        /// <summary>
        /// The videos found with that tag.
        /// </summary>
        public IEnumerable<VideoPreviewViewModel> Videos { get; set; }

        /// <summary>
        /// The paging state indicating whether there is a next page.
        /// </summary>
        public string PagingState { get; set; }
    }
}