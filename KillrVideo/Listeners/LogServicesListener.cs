﻿using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Grpc.Core;
using KillrVideo.Protobuf;
using KillrVideo.Protobuf.Services;
using Serilog;

namespace KillrVideo.Listeners
{
    /// <summary>
    /// Listener that logs started Grpc services to Serilog.
    /// </summary>
    [Export(typeof(IServerListener))]
    public class LogServicesListener : IServerListener
    {
        private static readonly ILogger Logger = Log.ForContext<LogServicesListener>();
        
        public void OnStart(IEnumerable<ServerPort> serverPorts, IEnumerable<IGrpcServerService> servicesStarted)
        {
            string[] servers = serverPorts.Select(s => $"{s.Host}:{s.Port}").ToArray();
            foreach (IGrpcServerService service in servicesStarted)
            {
                Logger.Information("Service {ServiceName} is listening on {ServerAddresses}", service.Descriptor.Name, servers);
            }
        }

        public void OnStop(IEnumerable<ServerPort> serverPorts, IEnumerable<IGrpcServerService> servicesStopped)
        {
        }
    }
}
