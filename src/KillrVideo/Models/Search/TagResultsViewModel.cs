using System;
using System.Collections.Generic;

namespace KillrVideo.Models.Search
{
    /// <summary>
    /// View model of tag suggestions.
    /// </summary>
    [Serializable]
    public class TagResultsViewModel
    {
        /// <summary>
        /// The start of the tag that was used to search.
        /// </summary>
        public string TagStart { get; set; }

        /// <summary>
        /// The tag suggestions.
        /// </summary>
        public IEnumerable<string> Tags { get; set; }
    }
}