using System;
using System.Collections.Generic;

namespace KillrVideo.Search.Dtos
{
    /// <summary>
    /// Represents a page of suggested queries for typeahead support.
    /// </summary>
    [Serializable]
    public class SuggestedQueries
    {
        public string Query { get; set; }
        public IEnumerable<string> Suggestions { get; set; }
    }
}