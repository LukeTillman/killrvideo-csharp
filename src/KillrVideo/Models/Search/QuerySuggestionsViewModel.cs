using System;
using System.Collections.Generic;

namespace KillrVideo.Models.Search
{
    /// <summary>
    /// View model of tag suggestions.
    /// </summary>
    [Serializable]
    public class QuerySuggestionsViewModel
    {
        /// <summary>
        /// The start of the tag that was used to search.
        /// </summary>
        public string Query { get; set; }

        /// <summary>
        /// The tag suggestions.
        /// </summary>
        public IEnumerable<string> Suggestions { get; set; }
    }
}