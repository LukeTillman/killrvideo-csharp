using System;

namespace KillrVideo.VideoCatalog.Dtos
{
    /// <summary>
    /// DTO for requesting videos for a user.
    /// </summary>
    [Serializable]
    public class GetUserVideos
    {
        public Guid UserId { get; set; }
        public int PageSize { get; set; }
        public string PagingState { get; set; }
    }
}