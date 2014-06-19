using System;

namespace KillrVideo.Models.Videos
{
    /// <summary>
    /// Request model for getting videos for a user.
    /// </summary>
    [Serializable]
    public class GetUserVideosViewModel
    {
        /// <summary>
        /// The user to get videos for.
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// The records per page.
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// The first video on the page to retrieve (will be null when requesting the first page).
        /// </summary>
        public VideoPreviewViewModel FirstVideoOnPage { get; set; }
    }
}