using System.Collections.Generic;
using Grpc.Core;
using KillrVideo.Protobuf.Services;

namespace KillrVideo.Protobuf
{
    /// <summary>
    /// Allows components to listen for start/stop events on a Grpc server.
    /// </summary>
    public interface IServerListener
    {
        /// <summary>
        /// Called when a Grpc server is started.
        /// </summary>
        void OnStart(IEnumerable<ServerPort> serverPorts, IEnumerable<IGrpcServerService> servicesStarted);

        /// <summary>
        /// Called when a Grpc server is stopped.
        /// </summary>
        void OnStop(IEnumerable<ServerPort> serverPorts, IEnumerable<IGrpcServerService> servicesStopped);
    }
}
