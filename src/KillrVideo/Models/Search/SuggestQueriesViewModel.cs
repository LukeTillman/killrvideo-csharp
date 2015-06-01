using System;

namespace KillrVideo.Models.Search
{
    /// <summary>
    /// View model for requesting tag suggestions.
    /// </summary>
    [Serializable]
    public class SuggestQueriesViewModel
    {
        /// <summary>
        /// The currently entered search text.
        /// </summary>
        public string Query { get; set; }

        /// <summary>
        /// The maximum number of tag suggestions to get.
        /// </summary>
        public int PageSize { get; set; }
    }
}