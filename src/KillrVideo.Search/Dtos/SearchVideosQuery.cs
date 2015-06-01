using System;

namespace KillrVideo.Search.Dtos
{
    /// <summary>
    /// Parameters for requesting a page of videos for a search query.
    /// </summary>
    [Serializable]
    public class SearchVideosQuery
    {
        public string Query { get; set; }
        public int PageSize { get; set; }
        public Guid? FirstVideoOnPageVideoId { get; set; }
    }
}