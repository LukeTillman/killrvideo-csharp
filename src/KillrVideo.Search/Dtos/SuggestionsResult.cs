using System;
using System.Collections.Generic;

namespace KillrVideo.Search.Dtos
{
    [Serializable]
    public class SuggestionsResult
    {
        public int NumFound { get; set; }
        public List<Suggestion> Suggestions { get; set; }
    }
}