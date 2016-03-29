using System;
using System.Threading.Tasks;
using Grpc.Core;
using KillrVideo.MessageBus;
using Serilog;

namespace KillrVideo.Ratings
{
    /// <summary>
    /// The RPC server for the Ratings service which can be started/stopped.
    /// </summary>
    public class RatingsRpcServer
    {
        private static readonly ILogger Logger = Log.ForContext<RatingsRpcServer>();

        private readonly Server _server;
        private IBusServer _bus;

        public RatingsRpcServer()
        {
            _server = new Server();
        }

        public void Start(RatingsRpcServerConfig config)
        {
            // Validate config
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (config.BusTransport == null) throw new ArgumentNullException(nameof(config.BusTransport));
            if (config.Cassandra == null) throw new ArgumentNullException(nameof(config.Cassandra));

            Logger.Information("Starting server");

            // Create the message bus
            _bus = BusBuilder.Configure().WithServiceName("KillrVideo.Ratings").WithTransport(config.BusTransport).Build();
            IBus publisher = _bus.StartServer();

            // Create, bind, and start the service endpoint
            var ratingsService = new RatingsServiceImpl(config.Cassandra, publisher);
            _server.Services.Add(RatingsService.BindService(ratingsService));
            _server.Ports.Add(config.Host, config.Port, ServerCredentials.Insecure);
            _server.Start();

            Logger.Information("Server started");
        }

        public async Task StopAsync()
        {
            Logger.Information("Stopping server");

            try
            {
                await _server.ShutdownAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Unexpected error while stopping RPC server");
            }

            try
            {
                if (_bus != null)
                    await _bus.StopServerAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Unexpected error while stopping bus");
            }

            Logger.Information("Server stopped");
        }
    }
}
