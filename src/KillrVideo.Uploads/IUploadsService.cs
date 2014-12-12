using System;
using System.Threading.Tasks;
using KillrVideo.Uploads.Dtos;

namespace KillrVideo.Uploads
{
    /// <summary>
    /// The public API for the uploaded videos service.
    /// </summary>
    public interface IUploadsService
    {
        /// <summary>
        /// Generates upload destinations for users to upload videos.
        /// </summary>
        Task<UploadDestination> GenerateUploadDestination(GenerateUploadDestination uploadDestination);

        /// <summary>
        /// Marks an upload as complete.
        /// </summary>
        Task MarkUploadComplete(MarkUploadComplete upload);

        /// <summary>
        /// Gets the status of an uploaded video's encoding job by the video Id.
        /// </summary>
        Task<EncodingJobProgress> GetStatusForVideo(Guid videoId);
    }
}
