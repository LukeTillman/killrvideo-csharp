using System;

namespace KillrVideo.Comments.Dtos
{
    /// <summary>
    /// Parameters for getting a page of comments by a user.
    /// </summary>
    [Serializable]
    public class GetUserComments
    {
        public Guid UserId { get; set; }
        public int PageSize { get; set; }

        /// <summary>
        /// The id of the first comment on the page.  Will be null when getting the first page of comments.
        /// </summary>
        public Guid? FirstCommentIdOnPage { get; set; }
    }
}
