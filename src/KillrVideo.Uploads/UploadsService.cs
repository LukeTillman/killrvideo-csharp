using System;
using System.Threading.Tasks;
using Grpc.Core;

namespace KillrVideo.Uploads
{
    /// <summary>
    /// The default implementation of the Uploads service currently doesn't support uploaded videos. (We need some way to support
    /// processing and extracting thumbnails locally in order to support this.)
    /// </summary>
    public class UploadsServiceImpl : UploadsService.IUploadsService
    {
        /// <summary>
        /// Generates upload destinations for users to upload videos.
        /// </summary>
        public Task<GetUploadDestinationResponse> GetUploadDestination(GetUploadDestinationRequest request, ServerCallContext context)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Marks an upload as complete.
        /// </summary>
        public Task<MarkUploadCompleteResponse> MarkUploadComplete(MarkUploadCompleteRequest request, ServerCallContext context)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Gets the status of an uploaded video's encoding job by the video Id.
        /// </summary>
        public Task<GetStatusOfVideoResponse> GetStatusOfVideo(GetStatusOfVideoRequest request, ServerCallContext context)
        {
            throw new NotSupportedException();
        }
    }
}