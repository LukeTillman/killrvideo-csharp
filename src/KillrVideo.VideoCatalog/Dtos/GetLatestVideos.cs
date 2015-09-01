using System;

namespace KillrVideo.VideoCatalog.Dtos
{
    /// <summary>
    /// Parameters for getting the latest videos
    /// </summary>
    [Serializable]
    public class GetLatestVideos
    {
        public int PageSize { get; set; }
        public string PagingState { get; set; }
    }
}
