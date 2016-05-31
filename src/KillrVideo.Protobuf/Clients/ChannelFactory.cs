using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf.Reflection;
using Grpc.Core;
using KillrVideo.Host.ServiceDiscovery;
using Serilog;

namespace KillrVideo.Protobuf.Clients
{
    /// <summary>
    /// Default Channel factory implementation that looks up services via service discovery and caches Channel instances
    /// by location (host/port) for reuse.
    /// </summary>
    [Export(typeof(IChannelFactory))]
    public class ChannelFactory : IChannelFactory, IDisposable
    {
        private static readonly ILogger Logger = Log.ForContext<ChannelFactory>();

        private readonly ConcurrentDictionary<string, Lazy<Channel>> _cache;
        private readonly IFindServices _serviceDiscovery;

        public ChannelFactory(IFindServices serviceDiscovery)
        {
            if (serviceDiscovery == null) throw new ArgumentNullException(nameof(serviceDiscovery));
            _serviceDiscovery = serviceDiscovery;

            _cache = new ConcurrentDictionary<string, Lazy<Channel>>();
        }

        public async Task<Channel> GetChannelAsync(ServiceDescriptor service)
        {
            // Find the service
            IEnumerable<string> locations = await _serviceDiscovery.LookupServiceAsync(service.Name).ConfigureAwait(false);
            string location = locations.FirstOrDefault();
            if (location == null)
                throw new ServiceNotFoundException(service);

            return _cache.GetOrAdd(location, CreateChannel).Value;
        }

        private static Lazy<Channel> CreateChannel(string location)
        {
            string[] locationParts = location.Split(':');
            if (locationParts.Length != 2)
                throw new ArgumentOutOfRangeException($"Location {location} does not contain host and port");

            string host = locationParts[0];
            int port = int.Parse(locationParts[1]);
            return new Lazy<Channel>(() => new Channel(host, port, ChannelCredentials.Insecure));
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
