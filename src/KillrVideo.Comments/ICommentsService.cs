using System.Threading.Tasks;
using KillrVideo.Comments.Dtos;

namespace KillrVideo.Comments
{
    /// <summary>
    /// Public API for the comments service.
    /// </summary>
    public interface ICommentsService
    {
        /// <summary>
        /// Records a user comment on a video.
        /// </summary>
        Task CommentOnVideo(CommentOnVideo comment);

        /// <summary>
        /// Gets a page of the latest comments for a user.
        /// </summary>
        Task<UserComments> GetUserComments(GetUserComments getComments);

        /// <summary>
        /// Gets a page of the latest comments for a video.
        /// </summary>
        Task<VideoComments> GetVideoComments(GetVideoComments getComments);
    }
}
