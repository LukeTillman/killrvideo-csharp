using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KillrVideo.VideoCatalog.ReadModel.Dtos;

namespace KillrVideo.VideoCatalog.ReadModel
{
    /// <summary>
    /// Does reads for the video catalog.
    /// </summary>
    public interface IVideoCatalogReadModel
    {
        /// <summary>
        /// Gets the details of a specific video.
        /// </summary>
        Task<VideoDetails> GetVideo(Guid videoId);

        /// <summary>
        /// Gets a limited number of video preview data by video id.
        /// </summary>
        Task<IEnumerable<VideoPreview>> GetVideoPreviews(ISet<Guid> videoIds);

        /// <summary>
        /// Gets the latest videos added to the site.
        /// </summary>
        Task<LatestVideos> GetLastestVideos(GetLatestVideos getVideos);

        /// <summary>
        /// Gets a page of videos for a particular user.
        /// </summary>
        Task<UserVideos> GetUserVideos(GetUserVideos getVideos);
    }
}