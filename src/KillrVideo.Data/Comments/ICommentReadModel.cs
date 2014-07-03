using System.Threading.Tasks;
using KillrVideo.Data.Comments.Dtos;

namespace KillrVideo.Data.Comments
{
    public interface ICommentReadModel
    {
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
