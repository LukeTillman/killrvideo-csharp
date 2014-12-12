using System;

namespace KillrVideo.Search.Dtos
{
    /// <summary>
    /// Parameters for requesting a page of videos by tag.
    /// </summary>
    [Serializable]
    public class GetVideosByTag
    {
        public string Tag { get; set; }
        public int PageSize { get; set; }
        public Guid? FirstVideoOnPageVideoId { get; set; }
    }
}