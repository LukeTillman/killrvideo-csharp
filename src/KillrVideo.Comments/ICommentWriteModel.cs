using System.Threading.Tasks;
using KillrVideo.Comments.Messages.Commands;

namespace KillrVideo.Comments
{
    public interface ICommentWriteModel
    {
        /// <summary>
        /// Adds a comment on a video.
        /// </summary>
        Task CommentOnVideo(CommentOnVideo comment);
    }
}