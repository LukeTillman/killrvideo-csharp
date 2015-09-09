using System;
using KillrVideo.Search.Dtos;
using System.Collections.Generic;
using Newtonsoft.Json;
namespace KillrVideo.Search.Dtos
{

    [Serializable]
    public class SearchSpellcheckSuggestions
    {

        public int NumFound { get; set; }

        public List<string> Suggestion { get; set; }
    }
}