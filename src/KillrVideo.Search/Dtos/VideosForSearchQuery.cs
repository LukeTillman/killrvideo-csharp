using System;
using System.Collections.Generic;

namespace KillrVideo.Search.Dtos
{
    /// <summary>
    /// Represents a page of videos for a search term.
    /// </summary>
    [Serializable]
    public class VideosForSearchQuery
    {
        public string Query { get; set; }
        public IEnumerable<VideoPreview> Videos { get; set; }
        public string PagingState { get; set; }
    }
}
