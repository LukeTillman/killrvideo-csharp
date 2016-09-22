using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using EtcdNet;
using Grpc.Core;
using KillrVideo.Configuration;
using KillrVideo.Host;
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
        private readonly BroadcastOptions _broadcastOptions;
        private readonly HostOptions _hostOptions;

        public RegisterWithEtcdListener(EtcdClient client, BroadcastOptions broadcastOptions, HostOptions hostOptions)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (broadcastOptions == null) throw new ArgumentNullException(nameof(broadcastOptions));
            if (hostOptions == null) throw new ArgumentNullException(nameof(hostOptions));
            _client = client;
            _broadcastOptions = broadcastOptions;
            _hostOptions = hostOptions;
        }

        public void OnStart(IEnumerable<ServerPort> serverPorts, IEnumerable<IGrpcServerService> servicesStarted)
        {
            // Register each service that started with etcd using the host IP setting
            var registerTasks = new List<Task>();
            foreach (IGrpcServerService service in servicesStarted)
            {
                var t = _client.SetNodeAsync(GetServiceKey(service), $"{_broadcastOptions.IP}:{_broadcastOptions.Port}");
                registerTasks.Add(t);
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
            return $"/killrvideo/services/{service.Descriptor.Name}/{_hostOptions.AppName}-{_hostOptions.AppInstance}";
        }
    }
}