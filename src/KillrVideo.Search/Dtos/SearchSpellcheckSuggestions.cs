using System;
using System.Collections.Generic;

namespace KillrVideo.Search.Dtos
{
    [Serializable]
    public class SearchSpellcheckSuggestions
    {
        public int NumFound { get; set; }
        public List<string> Suggestion { get; set; }
    }
}