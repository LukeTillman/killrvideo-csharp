using System.Threading.Tasks;
using KillrVideo.Data.Videos.Dtos;

namespace KillrVideo.Data.Videos
{
    public interface IVideoWriteModel
    {
        /// <summary>
        /// Adds a new video.
        /// </summary>
        Task AddVideo(AddVideo video);

        /// <summary>
        /// Adds a rating for a video.
        /// </summary>
        Task RateVideo(RateVideo videoRating);

        /// <summary>
        /// Renames a video.
        /// </summary>
        Task RenameVideo(RenameVideo renameVideo);

        /// <summary>
        /// Changes a video's description.
        /// </summary>
        Task ChangeVideoDescription(ChangeVideoDescription changeVideo);
    }
}