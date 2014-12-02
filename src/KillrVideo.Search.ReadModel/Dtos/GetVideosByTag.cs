using System;

namespace KillrVideo.Search.ReadModel.Dtos
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