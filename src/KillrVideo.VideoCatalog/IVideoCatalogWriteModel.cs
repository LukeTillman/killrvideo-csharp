using System;
using System.Threading.Tasks;
using KillrVideo.VideoCatalog.Messages.Commands;

namespace KillrVideo.VideoCatalog
{
    /// <summary>
    /// Does writes for the video catalog.
    /// </summary>
    public interface IVideoCatalogWriteModel
    {
        /// <summary>
        /// Adds a YouTube video to the catalog.
        /// </summary>
        Task AddYouTubeVideo(SubmitYouTubeVideo youTubeVideo);

        /// <summary>
        /// Adds an uploaded video to the catalog.
        /// </summary>
        Task AddUploadedVideo(SubmitUploadedVideo uploadedVideo);

        /// <summary>
        /// Updates an uploaded video with the playback location for the video and the thumbnail preview location.
        /// </summary>
        Task UpdateUploadedVideoLocations(Guid videoId, string videoLocation, string thumbnailLocation);
    }
}
