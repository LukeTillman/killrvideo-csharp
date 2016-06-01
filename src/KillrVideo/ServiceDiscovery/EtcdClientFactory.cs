using System.ComponentModel.Composition;
using DryIocAttributes;
using EtcdNet;
using KillrVideo.Configuration;
using KillrVideo.Host.Config;

namespace KillrVideo.ServiceDiscovery
{
    /// <summary>
    /// Static factory class for creating an Etcd client.
    /// </summary>
    [Export, AsFactory]
    public static class EtcdClientFactory
    {
        [Export]
        public static EtcdClient CreateEtcdClient(IHostConfiguration config)
        {
            string etcdHost = config.GetRequiredConfigurationValue(ConfigConstants.DockerIp);
            var opts = new EtcdClientOpitions()
            {
                Urls = new[] {$"http://{etcdHost}/killrvideo"}
            };
            return new EtcdClient(opts);
        }
    }
}
