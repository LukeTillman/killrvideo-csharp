using System.Threading.Tasks;
using KillrVideo.Data.Upload.Dtos;

namespace KillrVideo.Data.Upload
{
    /// <summary>
    /// Handles write operations related to uploaded videos.
    /// </summary>
    public interface IUploadedVideosWriteModel
    {
        /// <summary>
        /// Adds a new uploaded video.
        /// </summary>
        Task AddVideo(AddUploadedVideo video);

        /// <summary>
        /// Adds a notification about an encoding job.
        /// </summary>
        Task AddEncodingJobNotification(AddEncodingJobNotification notification);
    }
}
