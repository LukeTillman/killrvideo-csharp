using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using EtcdNet;
using KillrVideo.Host.ServiceDiscovery;

namespace KillrVideo.ServiceDiscovery
{
    /// <summary>
    /// Service discovery implementation using etcd as the service registry.
    /// </summary>
    [Export(typeof(IFindServices))]
    public class EtcdServiceDiscovery : IFindServices
    {
        private readonly EtcdClient _client;

        public EtcdServiceDiscovery(EtcdClient client)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            _client = client;
        }

        public async Task<IEnumerable<string>> LookupServiceAsync(string serviceName)
        {
            EtcdResponse response = await _client.GetNodeAsync($"/killrvideo/services/{serviceName}", ignoreKeyNotFoundException: true).ConfigureAwait(false);
            if (response == null)
                throw new ServiceNotFoundException(serviceName);

            return response.Node.Nodes.Select(n => n.Value);
        }
    }
}
