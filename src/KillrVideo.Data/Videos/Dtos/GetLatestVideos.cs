using System;

namespace KillrVideo.Data.Videos.Dtos
{
    /// <summary>
    /// Parameters for getting the latest videos
    /// </summary>
    [Serializable]
    public class GetLatestVideos
    {
        public int PageSize { get; set; }
        public Guid? FirstVideoOnPageVideoId { get; set; }
        public DateTimeOffset? FirstVideoOnPageDate { get; set; }
    }
}
