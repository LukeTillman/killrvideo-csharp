using System;
using System.Threading.Tasks;
using KillrVideo.Data.Comments.Dtos;

namespace KillrVideo.Data.Comments
{
    public interface ICommentWriteModel
    {
        /// <summary>
        /// Adds a comment on a video.  Returns an unique Id for the comment.
        /// </summary>
        Task<Guid> CommentOnVideo(CommentOnVideo comment);
    }
}