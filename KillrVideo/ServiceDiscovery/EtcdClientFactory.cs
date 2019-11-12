using System.ComponentModel.Composition;
using DryIocAttributes;
using EtcdNet;

namespace KillrVideo.ServiceDiscovery
{
    /// <summary>
    /// Static factory class for creating an Etcd client.
    /// </summary>
    [Export, AsFactory]
    public static class EtcdClientFactory
    {
        [Export]
        public static EtcdClient CreateEtcdClient(EtcdOptions options)
        {
            var opts = new EtcdClientOpitions()
            {
                Urls = new[] {$"http://{options.IP}:{options.Port}"}
            };
            return new EtcdClient(opts);
        }
    }
}
