using Grpc.Core;

namespace KillrVideo.Protobuf.Services
{
    /// <summary>
    /// Interface for components that are Grpc service implementations that can be converted to a ServerServiceDefintion and
    /// run on a Grpc Server instance.
    /// </summary>
    public interface IGrpcServerService
    {
        /// <summary>
        /// Converts the service implementation to a ServerServiceDefinition for running on a Grpc Server.
        /// </summary>
        ServerServiceDefinition ToServerServiceDefinition();
    }
}
