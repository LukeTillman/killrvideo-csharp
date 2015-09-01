using System;

namespace KillrVideo.SuggestedVideos.Dtos
{
    /// <summary>
    /// Represents request parameters for getting related videos.
    /// </summary>
    [Serializable]
    public class RelatedVideosQuery
    {
        /// <summary>
        /// The video Id to search for related videos.
        /// </summary>
        public Guid VideoId { get; set; }

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
