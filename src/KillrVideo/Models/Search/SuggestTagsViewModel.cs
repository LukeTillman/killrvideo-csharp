using System;

namespace KillrVideo.Models.Search
{
    /// <summary>
    /// View model for requesting tag suggestions.
    /// </summary>
    [Serializable]
    public class SuggestTagsViewModel
    {
        /// <summary>
        /// The beginning of the tag.
        /// </summary>
        public string TagStart { get; set; }

        /// <summary>
        /// The maximum number of tag suggestions to get.
        /// </summary>
        public int PageSize { get; set; }
    }
}