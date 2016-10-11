using System;

namespace KillrVideo.Search.Dtos
{
    [Serializable]
    public class SearchResult
    {
        public SuggestResult Suggest { get; set; }
    }
}