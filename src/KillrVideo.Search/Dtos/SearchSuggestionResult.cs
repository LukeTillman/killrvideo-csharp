using System;
using KillrVideo.Search.Dtos;
namespace KillrVideo.Search.Dtos
{

    [Serializable]
    public class SearchSuggestionResult
    {
        public SearchSpellcheck Spellcheck { get; set; }
    }
}