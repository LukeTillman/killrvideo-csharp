using System;
using Google.Protobuf.Reflection;
using KillrVideo.Host.Config;

namespace KillrVideo.Protobuf.ServiceDiscovery
{
    /// <summary>
    /// Service discovery implementation that just points all service clients to 'localhost' and the port
    /// where they have been configured to run locally.
    /// </summary>
    public class LocalServiceDiscovery : IFindGrpcServices
    {
        private readonly IHostConfiguration _hostConfig;
        private readonly Lazy<ServiceLocation> _localServicesIp; 

        public LocalServiceDiscovery(IHostConfiguration hostConfig)
        {
            if (hostConfig == null) throw new ArgumentNullException(nameof(hostConfig));
            _hostConfig = hostConfig;
            _localServicesIp = new Lazy<ServiceLocation>(GetLocalGrpcServer);
        }

        public ServiceLocation Find(ServiceDescriptor service)
        {
            return _localServicesIp.Value;
        }

        private ServiceLocation GetLocalGrpcServer()
        {
            // Get the host/port configuration for the Grpc Server
            string host = "localhost";
            string portVal = _hostConfig.GetRequiredConfigurationValue(GrpcServerTask.HostPortKey);
            int port = int.Parse(portVal);

            return new ServiceLocation(host, port);
        }
    }
}
