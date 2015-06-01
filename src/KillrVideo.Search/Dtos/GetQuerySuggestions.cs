using System;

namespace KillrVideo.Search.Dtos
{
    /// <summary>
    /// Parameters for getting search query suggestions.
    /// </summary>
    [Serializable]
    public class GetQuerySuggestions
    {
        public string Query { get; set; }
        public int PageSize { get; set; }
    }
}