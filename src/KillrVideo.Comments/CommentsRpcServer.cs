using System;
using System.Threading.Tasks;
using Grpc.Core;
using KillrVideo.MessageBus;
using Serilog;

namespace KillrVideo.Comments
{
    /// <summary>
    /// The RPC endpoint server for the Comments Service which can be started/stopped.
    /// </summary>
    public class CommentsRpcServer
    {
        private static readonly ILogger Logger = Log.ForContext<CommentsRpcServer>();

        private readonly Server _server;
        private IBusServer _bus;

        public CommentsRpcServer()
        {
            _server = new Server();
        }

        public void Start(CommentsRpcServerConfig config)
        {
            // Validate config
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (config.BusTransport == null) throw new ArgumentNullException(nameof(config.BusTransport));
            if (config.Cassandra == null) throw new ArgumentNullException(nameof(config.Cassandra));

            // Create the message bus
            _bus = BusBuilder.Configure().WithServiceName("KillrVideo.Comments").WithTransport(config.BusTransport).Build();
            IBus publisher = _bus.StartServer();

            // Create, bind, and start the service endpoint
            var commentsService = new CommentsServiceImpl(config.Cassandra, publisher);
            _server.Services.Add(CommentsService.BindService(commentsService));
            _server.Ports.Add(config.Host, config.Port, ServerCredentials.Insecure);
            _server.Start();
        }

        public async Task StopAsync()
        {
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
        }
    }
}
