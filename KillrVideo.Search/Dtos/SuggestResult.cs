using System;
using System.Collections.Generic;

namespace KillrVideo.Search.Dtos
{
    [Serializable]
    public class SuggestResult
    {
        public Dictionary<string, SuggestionsResult> SearchSuggester { get; set; }
    }
}