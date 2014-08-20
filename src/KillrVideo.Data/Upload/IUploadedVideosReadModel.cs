using System;
using System.Threading.Tasks;
using KillrVideo.Data.Upload.Dtos;

namespace KillrVideo.Data.Upload
{
    /// <summary>
    /// Handles read operations related to uploaded videos.
    /// </summary>
    public interface IUploadedVideosReadModel
    {
        /// <summary>
        /// Gets an uploaded video's details by the video id.
        /// </summary>
        Task<UploadedVideo> GetByVideoId(Guid videoId);

        /// <summary>
        /// Gets an uploaded video's details by the encoding job's id.
        /// </summary>
        Task<UploadedVideo> GetByJobId(string jobId);

        /// <summary>
        /// Gets the status of an uploaded video's encoding job by the job Id.
        /// </summary>
        Task<EncodingJobProgress> GetStatusForJob(string jobId);
    }
}