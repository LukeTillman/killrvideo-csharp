using System;

namespace KillrVideo.Models.Videos
{
    /// <summary>
    /// Request for getting recommended videos for the currently logged in user.
    /// </summary>
    [Serializable]
    public class GetRecommendedVideosViewModel
    {
        /// <summary>
        /// The number of records per page.
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// The current paging state. Will be null on initial load, then may have a value for subsequent pages.
        /// </summary>
        public string PagingState { get; set; }
    }
}