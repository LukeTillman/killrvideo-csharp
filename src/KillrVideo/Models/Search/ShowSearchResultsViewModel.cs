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
        /// The tag to show results for.
        /// </summary>
        public string Tag { get; set; }
    }
}