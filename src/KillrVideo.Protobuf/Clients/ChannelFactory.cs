using System;
using System.Collections.Concurrent;
using System.ComponentModel.Composition;
using System.Net;
using Google.Protobuf.Reflection;
using Grpc.Core;
using KillrVideo.Protobuf.ServiceDiscovery;

namespace KillrVideo.Protobuf.Clients
{
    /// <summary>
    /// Default Channel factory implementation that looks up services via service discovery and caches Channel instances
    /// by IPEndPoint for reuse.
    /// </summary>
    [Export(typeof(IChannelFactory))]
    public class ChannelFactory : IChannelFactory
    {
        private readonly ConcurrentDictionary<IPEndPoint, Lazy<Channel>> _cache;
        private readonly IFindGrpcServices _serviceDiscovery;

        public ChannelFactory(IFindGrpcServices serviceDiscovery)
        {
            if (serviceDiscovery == null) throw new ArgumentNullException(nameof(serviceDiscovery));
            _serviceDiscovery = serviceDiscovery;

            _cache = new ConcurrentDictionary<IPEndPoint, Lazy<Channel>>();
        }

        public Channel GetChannel(ServiceDescriptor service)
        {
            // Find the service
            IPEndPoint ip = _serviceDiscovery.Find(service);
            if (ip == null)
                throw new ServiceNotFoundException(service);

            return _cache.GetOrAdd(ip, CreateChannel).Value;
        }

        private static Lazy<Channel> CreateChannel(IPEndPoint ip)
        {
            return new Lazy<Channel>(() => new Channel(ip.Address.ToString(), ip.Port, ChannelCredentials.Insecure));
        }
    }
}
