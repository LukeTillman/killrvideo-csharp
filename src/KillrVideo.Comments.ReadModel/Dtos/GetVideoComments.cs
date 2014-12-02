using System;

namespace KillrVideo.Comments.ReadModel.Dtos
{
    /// <summary>
    /// Parameters for getting a page of comments by video.
    /// </summary>
    [Serializable]
    public class GetVideoComments
    {
        public Guid VideoId { get; set; }
        public int PageSize { get; set; }

        /// <summary>
        /// The id of the first comment on the page.  Will be null when getting the first page of comments.
        /// </summary>
        public Guid? FirstCommentIdOnPage { get; set; }
    }
}