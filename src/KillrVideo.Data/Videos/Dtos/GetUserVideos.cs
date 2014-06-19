using System;

namespace KillrVideo.Data.Videos.Dtos
{
    /// <summary>
    /// DTO for requesting videos for a user.
    /// </summary>
    [Serializable]
    public class GetUserVideos
    {
        public Guid UserId { get; set; }
        public int PageSize { get; set; }

        /// <summary>
        /// The added date of the first video on the page (will be null when requesting the first page).
        /// </summary>
        public DateTimeOffset? FirstVideoOnPageAddedDate { get; set; }

        /// <summary>
        /// The Id of the first video on the page (will be null when requesting the first page).
        /// </summary>
        public Guid? FirstVideoOnPageVideoId { get; set; }
    }
}