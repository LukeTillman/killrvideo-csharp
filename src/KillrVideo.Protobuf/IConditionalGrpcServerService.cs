using System.Collections.Generic;

namespace KillrVideo.Protobuf
{
    /// <summary>
    /// Interface for Grpc services that may or may not run based on the host's configuration.
    /// </summary>
    public interface IConditionalGrpcServerService : IGrpcServerService
    {
        /// <summary>
        /// Returns true if this service should run given the configuration of the host.
        /// </summary>
        bool ShouldRun(IDictionary<string, string> hostConfiguration);
    }
}
