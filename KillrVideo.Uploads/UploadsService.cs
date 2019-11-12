using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Google.Protobuf.Reflection;
using Grpc.Core;
using KillrVideo.Protobuf.Services;

namespace KillrVideo.Uploads
{
    /// <summary>
    /// The default implementation of the Uploads service currently doesn't support uploaded videos. (We need some way to support
    /// processing and extracting thumbnails locally in order to support this.)
    /// </summary>
    [Export(typeof(IGrpcServerService))]
    public class UploadsServiceImpl : UploadsService.UploadsServiceBase, IGrpcServerService
    {
        public ServiceDescriptor Descriptor => UploadsService.Descriptor;

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
        public override Task<GetUploadDestinationResponse> GetUploadDestination(GetUploadDestinationRequest request, ServerCallContext context)
        {
            var status = new Status(StatusCode.Unimplemented, "Uploading videos is currently not supported");
            throw new RpcException(status);
        }

        /// <summary>
        /// Marks an upload as complete.
        /// </summary>
        public override Task<MarkUploadCompleteResponse> MarkUploadComplete(MarkUploadCompleteRequest request, ServerCallContext context)
        {
            var status = new Status(StatusCode.Unimplemented, "Uploading videos is currently not supported");
            throw new RpcException(status);
        }

        /// <summary>
        /// Gets the status of an uploaded video's encoding job by the video Id.
        /// </summary>
        public override Task<GetStatusOfVideoResponse> GetStatusOfVideo(GetStatusOfVideoRequest request, ServerCallContext context)
        {
            var status = new Status(StatusCode.Unimplemented, "Uploading videos is currently not supported");
            throw new RpcException(status);
        }
    }
}