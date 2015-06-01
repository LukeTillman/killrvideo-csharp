using System;

namespace KillrVideo.Models.Search
{
    /// <summary>
    /// ViewModel for the search results view.
    /// </summary>
    [Serializable]
    public class ShowSearchResultsViewModel
    {
        /// <summary>
        /// The query to show results for.
        /// </summary>
        public string Query { get; set; }
    }
}