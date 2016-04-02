using System;
using System.Net;
using Google.Protobuf.Reflection;
using KillrVideo.Host.Config;

namespace KillrVideo.Protobuf.ServiceDiscovery
{
    /// <summary>
    /// Service discovery implementation that just points all service clients to the host/port where they
    /// have been configured to run locally.
    /// </summary>
    public class LocalServiceDiscovery : IFindGrpcServices
    {
        private readonly IHostConfiguration _hostConfig;
        private readonly Lazy<IPEndPoint> _localServicesIp; 

        public LocalServiceDiscovery(IHostConfiguration hostConfig)
        {
            if (hostConfig == null) throw new ArgumentNullException(nameof(hostConfig));
            _hostConfig = hostConfig;
            _localServicesIp = new Lazy<IPEndPoint>(GetLocalServicesIp);
        }

        public IPEndPoint Find(ServiceDescriptor service)
        {
            return _localServicesIp.Value;
        }

        private IPEndPoint GetLocalServicesIp()
        {
            // Get the host/port configuration for the Grpc Server
            string host = _hostConfig.GetRequiredConfigurationValue(GrpcServerTask.HostConfigKey);
            string portVal = _hostConfig.GetRequiredConfigurationValue(GrpcServerTask.HostPortKey);
            int port = int.Parse(portVal);
            return new IPEndPoint(IPAddress.Parse(host), port);
        }
    }
}
