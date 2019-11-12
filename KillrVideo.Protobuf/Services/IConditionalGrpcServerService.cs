﻿namespace KillrVideo.Protobuf.Services
{
    /// <summary>
    /// Interface for Grpc services that may or may not run based on the host's configuration.
    /// </summary>
    public interface IConditionalGrpcServerService : IGrpcServerService
    {
        /// <summary>
        /// Returns true if this service should run.
        /// </summary>
        bool ShouldRun();
    }
}
