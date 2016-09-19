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
        private readonly Lazy<string> _uniqueId;

        public RegisterWithEtcdListener(EtcdClient client, IHostConfiguration config)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (config == null) throw new ArgumentNullException(nameof(config));
            _client = client;
            _config = config;
            _uniqueId = new Lazy<string>(() => $"{_config.ApplicationName}:{_config.ApplicationInstanceId}");
        }

        public void OnStart(IEnumerable<ServerPort> serverPorts, IEnumerable<IGrpcServerService> servicesStarted)
        {
            string ip = _config.GetRequiredConfigurationValue(ConfigConstants.HostIp);
        
            int[] ports = serverPorts.Select(p => p.Port).ToArray();

            // Register each service that started with etcd using the host IP setting
            var registerTasks = new List<Task>();
            foreach (IGrpcServerService service in servicesStarted)
            {
                foreach (int port in ports)
                {
                    var t = _client.SetNodeAsync(GetServiceKey(service), $"{ip}:{port}");
                    registerTasks.Add(t);
                }
            }
            
            Task.WaitAll(registerTasks.ToArray());
        }

        public void OnStop(IEnumerable<ServerPort> serverPorts, IEnumerable<IGrpcServerService> servicesStopped)
        {
            // Remove all services from etcd that were registered on startup
            var removeTasks = new List<Task>();
            foreach (IGrpcServerService service in servicesStopped)
            {
                var t = _client.DeleteNodeAsync(GetServiceKey(service));
                removeTasks.Add(t);
            }

            Task.WaitAll(removeTasks.ToArray());
        }

        private string GetServiceKey(IGrpcServerService service)
        {
            return $"/killrvideo/services/{service.Descriptor.Name}/{_uniqueId.Value}";
        }
    }
}