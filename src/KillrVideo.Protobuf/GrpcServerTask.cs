using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using KillrVideo.Host.Config;
using KillrVideo.Host.Tasks;
using KillrVideo.Protobuf.Services;
using Serilog;

namespace KillrVideo.Protobuf
{
    /// <summary>
    /// A Host Task that will start/stop a Grpc server for any services found.
    /// </summary>
    [Export(typeof(IHostTask))]
    public class GrpcServerTask : IHostTask
    {
        public const string HostConfigKey = "Grpc.Host";
        public const string HostPortKey = "Grpc.Port";

        private static readonly ILogger Logger = Log.ForContext<GrpcServerTask>();

        private readonly IEnumerable<IGrpcServerService> _availableServices;
        private readonly IHostConfiguration _hostConfiguration;
        private readonly Server _server;
        private readonly List<IGrpcServerService> _startedServices;

        public string Name => "Grpc Server";

        public IEnumerable<IServerListener> Listeners { get; set; }

        public GrpcServerTask(IEnumerable<IGrpcServerService> availableServices, IHostConfiguration hostConfiguration)
        {
            if (availableServices == null) throw new ArgumentNullException(nameof(availableServices));
            if (hostConfiguration == null) throw new ArgumentNullException(nameof(hostConfiguration));
            _availableServices = availableServices;
            _hostConfiguration = hostConfiguration;

            GrpcEnvironment.SetLogger(new SerilogGrpcLogger(Log.Logger));
            _server = new Server();
            _startedServices = new List<IGrpcServerService>();
            Listeners = Enumerable.Empty<IServerListener>();
        }

        public void Start()
        {
            // Get the host/port configuration for the Grpc Server
            string host = _hostConfiguration.GetRequiredConfigurationValue(HostConfigKey);
            string portVal = _hostConfiguration.GetRequiredConfigurationValue(HostPortKey);
            int port = int.Parse(portVal);

            _server.Ports.Add(host, port, ServerCredentials.Insecure);

            // Add services to the server
            foreach (IGrpcServerService service in _availableServices)
            {
                string serviceTypeName = service.GetType().Name;
                Logger.Debug("Found GrpcServerService {ServiceTypeName}", serviceTypeName);

                var conditionalService = service as IConditionalGrpcServerService;
                bool shouldRun = conditionalService?.ShouldRun(_hostConfiguration) ?? true;
                if (shouldRun)
                {
                    Logger.Debug("Adding GrpcServerService {ServiceTypeName}", serviceTypeName);
                    _startedServices.Add(service);
                    _server.Services.Add(service.ToServerServiceDefinition());
                }
            }

            if (_startedServices.Count == 0)
                throw new InvalidOperationException("No services found to start");

            // Start the server
            Logger.Information("Starting Grpc Server on {Host}:{Port} with {ServicesCount} services", host, port, _startedServices.Count);
            _server.Start();
            OnStart();
            Logger.Information("Started Grpc Server");
        }

        public async Task StopAsync()
        {
            // Stop the server
            Logger.Information("Stopping Grpc Server");
            try
            {
                await _server.ShutdownAsync().ConfigureAwait(false);
                OnStop();
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error while stopping Grpc Server");
            }
            Logger.Information("Stopped Grpc Server");
        }

        private void OnStart()
        {
            foreach (IServerListener listener in Listeners)
            {
                Logger.Debug("Running listener {ListenerTypeName}", listener.GetType().Name);
                try
                {
                    listener.OnStart(_server.Ports, _startedServices);
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Error while running server listener OnStart");
                }
            }
        }

        private void OnStop()
        {
            foreach (IServerListener listener in Listeners)
            {
                Logger.Debug("Running listener {ListenerTypeName}", listener.GetType().Name);
                try
                {
                    listener.OnStop(_server.Ports, _startedServices);
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Error while running server listern OnStop");
                }
            }
        }
    }
}
