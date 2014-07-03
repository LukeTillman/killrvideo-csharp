using System;

namespace KillrVideo.Models.Comments
{
    /// <summary>
    /// Request model for getting comments by video.
    /// </summary>
    [Serializable]
    public class GetVideoCommentsViewModel
    {
        /// <summary>
        /// The video to get comments for.
        /// </summary>
        public Guid VideoId { get; set; }

        /// <summary>
        /// The number of comments to retrieve.
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// The Id of the first comment on the page.  Can be null when retrieving the first page.
        /// </summary>
        public Guid? FirstCommentIdOnPage { get; set; }
    }
}