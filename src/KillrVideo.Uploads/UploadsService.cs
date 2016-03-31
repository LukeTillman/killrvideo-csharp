using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Grpc.Core;
using KillrVideo.Protobuf;

namespace KillrVideo.Uploads
{
    /// <summary>
    /// The default implementation of the Uploads service currently doesn't support uploaded videos. (We need some way to support
    /// processing and extracting thumbnails locally in order to support this.)
    /// </summary>
    [Export(typeof(IGrpcServerService))]
    public class UploadsServiceImpl : UploadsService.IUploadsService, IGrpcServerService
    {
        /// <summary>
        /// Convert this instance to a ServerServiceDefinition that can be run on a Grpc server.
        /// </summary>
        public ServerServiceDefinition ToServerServiceDefinition()
        {
            return UploadsService.BindService(this);
        }

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