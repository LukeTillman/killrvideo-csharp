using System;
using System.Collections.Generic;

namespace KillrVideo.Search.Dtos
{
    /// <summary>
    /// Represents a page of tags starting with specified text.
    /// </summary>
    [Serializable]
    public class TagsStartingWith
    {
        public string TagStartsWith { get; set; }
        public IEnumerable<string> Tags { get; set; }
    }
}