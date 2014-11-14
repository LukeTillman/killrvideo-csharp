using System;
using System.Threading.Tasks;
using KillrVideo.Comments.Messages.Commands;

namespace KillrVideo.Comments
{
    public interface ICommentWriteModel
    {
        /// <summary>
        /// Adds a comment on a video.  Returns an unique Id for the comment.
        /// </summary>
        Task<Guid> CommentOnVideo(CommentOnVideo comment);
    }
}