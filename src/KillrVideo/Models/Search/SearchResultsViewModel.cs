using System;
using System.Collections.Generic;
using KillrVideo.Models.Shared;
using KillrVideo.Models.Videos;

namespace KillrVideo.Models.Search
{
    /// <summary>
    /// Results of searching videos by tag.
    /// </summary>
    [Serializable]
    public class SearchResultsViewModel
    {
        /// <summary>
        /// The tag that was searched.
        /// </summary>
        public string Tag { get; set; }

        /// <summary>
        /// The videos found with that tag.
        /// </summary>
        public IEnumerable<VideoPreviewViewModel> Videos { get; set; }
    }
}