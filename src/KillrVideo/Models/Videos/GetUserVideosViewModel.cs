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
        /// The paging state. Will be null on initial page requests, then may have a value on subsequent requests.
        /// </summary>
        public string PagingState { get; set; }
    }
}