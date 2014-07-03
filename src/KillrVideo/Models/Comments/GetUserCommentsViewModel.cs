using System;

namespace KillrVideo.Models.Comments
{
    /// <summary>
    /// Request model for getting comments by user.
    /// </summary>
    [Serializable]
    public class GetUserCommentsViewModel
    {
        /// <summary>
        /// The user to get comments for.
        /// </summary>
        public Guid? UserId { get; set; }

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