using System;
using System.Configuration;
using System.Net;
using System.Reflection;
using Cassandra;
using DryIoc;
using DryIoc.MefAttributedModel;
using Grpc.Core;
using KillrVideo.Cassandra;
using KillrVideo.Comments;
using KillrVideo.MessageBus;
using KillrVideo.MessageBus.Transport;
using KillrVideo.Ratings;
using KillrVideo.Statistics;
using KillrVideo.SuggestedVideos;
using KillrVideo.Uploads;
using KillrVideo.UserManagement;
using RestSharp;
using Serilog;
using Serilog.Events;

namespace KillrVideo
{
    /// <summary>
    /// Console application for running/debugging all the KillrVideo backend services in-process together.
    /// </summary>
    class Program
    {
        private static readonly Assembly[] ServiceAssemblies = new[]
        {
            typeof (CommentsService).Assembly,
            typeof (RatingsService).Assembly,
            typeof (StatisticsService).Assembly,
            typeof (SuggestedVideoService).Assembly,
            typeof (UploadsService).Assembly,
            typeof (UserManagementServiceFactory).Assembly
        };

        static void Main(string[] args)
        {
            // Configure logging
            Log.Logger = new LoggerConfiguration()
                .WriteTo.ColoredConsole(LogEventLevel.Information, "{Timestamp:HH:mm:ss} [{SourceContext:l}] {Message}{NewLine}{Exception}")
                .CreateLogger();

            // Get the host and starting port to bind RPC services to
            string host = ConfigurationManager.AppSettings.Get("ServicesHost");
            if (string.IsNullOrWhiteSpace(host))
                throw new InvalidOperationException("You must specify the ServicesHost configuration option");

            string portConfig = ConfigurationManager.AppSettings.Get("ServicesPort");
            if (string.IsNullOrWhiteSpace(portConfig))
                throw new InvalidOperationException("You must specify the ServicesPort configuration option");

            int port = int.Parse(portConfig);

            // Create IoC container
            IContainer container = CreateContainer();

            // Let the container pick up any components using the MEF-like attributes in referenced assemblies (this will pick up any 
            // exported Grpc server definitions, message bus handlers, and background tasks in the referenced services)
            container = container.WithMefAttributedModel();
            container.RegisterExports(ServiceAssemblies);

            // Create a Grpc server with any services from the container
            var server = new Server();
            server.Ports.Add(host, port, ServerCredentials.Insecure);

            foreach (var serverServiceDef in container.ResolveMany<ServerServiceDefinition>())
                server.Services.Add(serverServiceDef);

            server.Start();
        }

        private static Container CreateContainer()
        {
            var container = new Container();

            // Register Cassandra session factory as singleton
            container.Register(Made.Of(() => CreateCassandraSession()), Reuse.Singleton);
            container.Register<PreparedStatementCache>(Reuse.Singleton);
            container.Register(Made.Of(() => GetDataStaxEnterpriseState()), Reuse.Singleton);

            // Register Bus and components
            container.Register(Made.Of(() => CreateBusServer()), Reuse.Singleton);
            container.Register(Made.Of(r => ServiceInfo.Of<IBusServer>(), busServer => busServer.StartServer()), Reuse.Singleton);

            // Register REST client
            container.Register<IRestClient, RestClient>(Made.Of(() => new RestClient()));

            return container;
        }

        private static ISession CreateCassandraSession()
        {
            string[] hosts = ConfigurationManager.AppSettings.Get("CassandraHosts").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (hosts.Length == 0)
                throw new InvalidOperationException("You must specify the CassandraHosts configuration option");

            Builder builder = Cluster.Builder();

            // Allow for multiple hosts
            foreach (string host in hosts)
            {
                string[] hostAndPort = host.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                switch (hostAndPort.Length)
                {
                    case 1:
                        builder.AddContactPoint(hostAndPort[0]);
                        break;
                    case 2:
                        builder.AddContactPoint(new IPEndPoint(IPAddress.Parse(hostAndPort[0]), int.Parse(hostAndPort[1])));
                        break;
                    default:
                        throw new InvalidOperationException($"Unable to parse host {host} from CassandraHosts configuration option");
                }
            }

            return builder.Build().Connect("killrvideo");
        }

        private static DataStaxEnterpriseEnvironmentState GetDataStaxEnterpriseState()
        {
            // TODO: Check configs for enabling DSE search/spark
            var state = DataStaxEnterpriseEnvironmentState.None;
            return state;
        }

        private static IBusServer CreateBusServer()
        {
            return BusBuilder.Configure().WithServiceName("KillrVideo").WithTransport(InMemoryTransport.Instance).Build();
        }
    }
}
