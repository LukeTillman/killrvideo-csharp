using System;
using KillrVideo.Search.Dtos;
using System.Collections.Generic;
namespace KillrVideo.Search.Dtos
{

    [Serializable]
    public class SearchSpellcheck
    {
        public List<string> Suggestions { get; set; }
    }
}