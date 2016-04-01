using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Grpc.Core;
using KillrVideo.Host.Config;
using KillrVideo.Host.Tasks;
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

        private readonly IEnumerable<IGrpcServerService> _services;
        private readonly Server _server;

        public string Name => "Grpc Server";

        public GrpcServerTask(IEnumerable<IGrpcServerService> services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            _services = services;

            _server = new Server();
        }

        public void Start(IHostConfiguration hostConfiguration)
        {
            // Get the host/port configuration for the Grpc Server
            string host = hostConfiguration.GetRequiredConfigurationValue(HostConfigKey);
            string portVal = hostConfiguration.GetRequiredConfigurationValue(HostPortKey);
            int port = int.Parse(portVal);

            _server.Ports.Add(host, port, ServerCredentials.Insecure);

            // Add services to the server
            int servicesCount = 0;
            foreach (IGrpcServerService service in _services)
            {
                string serviceTypeName = service.GetType().Name;
                Logger.Debug("Found GrpcServerService {ServiceTypeName}", serviceTypeName);

                var conditionalService = service as IConditionalGrpcServerService;
                bool shouldRun = conditionalService?.ShouldRun(hostConfiguration) ?? true;
                if (shouldRun)
                {
                    Logger.Debug("Adding GrpcServerService {ServiceTypeName}", serviceTypeName);
                    _server.Services.Add(service.ToServerServiceDefinition());
                    servicesCount++;
                }
            }

            if (servicesCount == 0)
                throw new InvalidOperationException("No services found to start");

            // Start the server
            Logger.Information("Starting Grpc Server on {Host}:{Port} with {ServicesCount} services", host, port, servicesCount);
            _server.Start();
            Logger.Information("Started Grpc Server");
        }

        public async Task StopAsync()
        {
            // Stop the server
            Logger.Information("Stopping Grpc Server");
            try
            {
                await _server.ShutdownAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error while stopping Grpc Server");
            }
            Logger.Information("Stopped Grpc Server");
        }
    }
}
