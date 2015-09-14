using System;

namespace KillrVideo.SuggestedVideos.Dtos
{
    /// <summary>
    /// Query parameters for retrieving suggested videos.
    /// </summary>
    [Serializable]
    public class SuggestedVideosQuery
    {
        /// <summary>
        /// The unique Id of the user to suggested videos for.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// The number of records per page.
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// A string token representing the paging state.
        /// </summary>
        public string PagingState { get; set; }
    }
}