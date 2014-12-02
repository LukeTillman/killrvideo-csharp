using System;
using System.Threading.Tasks;
using KillrVideo.Uploads.ReadModel.Dtos;

namespace KillrVideo.Uploads.ReadModel
{
    /// <summary>
    /// Handles read operations related to uploaded videos.
    /// </summary>
    public interface IUploadedVideosReadModel
    {
        /// <summary>
        /// Gets the status of an uploaded video's encoding job by the video Id.
        /// </summary>
        Task<EncodingJobProgress> GetStatusForVideo(Guid videoId);
    }
}