using System.Threading.Tasks;
using KillrVideo.VideoCatalog.Api.Commands;

namespace KillrVideo.VideoCatalog
{
    /// <summary>
    /// Does writes for the video catalog.
    /// </summary>
    public interface IVideoCatalogWriteModel
    {
        /// <summary>
        /// Adds a new video.
        /// </summary>
        Task AddVideo(AddVideo video);
    }
}
