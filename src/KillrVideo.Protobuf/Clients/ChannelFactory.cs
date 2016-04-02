using System;
using System.Collections.Concurrent;
using System.ComponentModel.Composition;
using Google.Protobuf.Reflection;
using Grpc.Core;
using KillrVideo.Protobuf.ServiceDiscovery;
using Serilog;

namespace KillrVideo.Protobuf.Clients
{
    /// <summary>
    /// Default Channel factory implementation that looks up services via service discovery and caches Channel instances
    /// by IPEndPoint for reuse.
    /// </summary>
    [Export(typeof(IChannelFactory))]
    public class ChannelFactory : IChannelFactory, IDisposable
    {
        private static readonly ILogger Logger = Log.ForContext<ChannelFactory>();

        private readonly ConcurrentDictionary<ServiceLocation, Lazy<Channel>> _cache;
        private readonly IFindGrpcServices _serviceDiscovery;

        public ChannelFactory(IFindGrpcServices serviceDiscovery)
        {
            if (serviceDiscovery == null) throw new ArgumentNullException(nameof(serviceDiscovery));
            _serviceDiscovery = serviceDiscovery;

            _cache = new ConcurrentDictionary<ServiceLocation, Lazy<Channel>>();
        }

        public Channel GetChannel(ServiceDescriptor service)
        {
            // Find the service
            ServiceLocation location = _serviceDiscovery.Find(service);
            if (location == null)
                throw new ServiceNotFoundException(service);

            return _cache.GetOrAdd(location, CreateChannel).Value;
        }

        private static Lazy<Channel> CreateChannel(ServiceLocation location)
        {
            return new Lazy<Channel>(() => new Channel(location.Host, location.Port, ChannelCredentials.Insecure));
        }

        public void Dispose()
        {
            // Make sure all channels are shutdown when factory is disposed of
            foreach (Lazy<Channel> channel in _cache.Values)
            {
                try
                {
                    channel.Value.ShutdownAsync();
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Exception while calling ShutdownAsync on Channel");
                }
            }
        }
    }
}
