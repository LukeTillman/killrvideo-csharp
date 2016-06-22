using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using EtcdNet;
using Grpc.Core;
using KillrVideo.Configuration;
using KillrVideo.Host.Config;
using KillrVideo.Protobuf;
using KillrVideo.Protobuf.Services;

namespace KillrVideo.Listeners
{
    /// <summary>
    /// Registers running services with Etcd for service discovery.
    /// </summary>
    [Export(typeof(IServerListener))]
    public class RegisterWithEtcdListener : IServerListener
    {
        private readonly EtcdClient _client;
        private readonly IHostConfiguration _config;

        public RegisterWithEtcdListener(EtcdClient client, IHostConfiguration config)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (config == null) throw new ArgumentNullException(nameof(config));
            _client = client;
            _config = config;
        }

        public void OnStart(IEnumerable<ServerPort> serverPorts, IEnumerable<IGrpcServerService> servicesStarted)
        {
            string ip = _config.GetRequiredConfigurationValue(ConfigConstants.HostIp);
            string uniqueId = $"{_config.ApplicationName}:{_config.ApplicationInstanceId}";
        
            int[] ports = serverPorts.Select(p => p.Port).ToArray();

            // Register each service that started with etcd using the host IP setting
            var registerTasks = new List<Task>();
            foreach (IGrpcServerService service in servicesStarted)
            {
                foreach (int port in ports)
                {
                    var t = _client.SetNodeAsync($"/killrvideo/services/{service.Descriptor.Name}/{uniqueId}", $"{ip}:{port}");
                    registerTasks.Add(t);
                }
            }
            
            Task.WaitAll(registerTasks.ToArray());
        }

        public void OnStop(IEnumerable<ServerPort> serverPorts, IEnumerable<IGrpcServerService> servicesStopped)
        {
        }
    }
}