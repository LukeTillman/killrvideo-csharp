// The reference to KillrVideo.SampleData is aliased because it has namespaces/types from Protos (since it consumes other services' protos)
// that collide with others defined in the services that own them. For example, it has KillrVideo.Comments types since it consumes the
// Comments service to post sample comments, which would conflict with the same types defined in the KillrVideo.Comments service project/DLL
// which is also referenced by this project
extern alias SampleData;

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
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
using KillrVideo.Protobuf;
using KillrVideo.Ratings;
using KillrVideo.Search;
using KillrVideo.Statistics;
using KillrVideo.SuggestedVideos;
using KillrVideo.Uploads;
using KillrVideo.UserManagement;
using KillrVideo.VideoCatalog;
using RestSharp;
using SampleData::KillrVideo.SampleData;
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
            typeof (UserManagementService).Assembly,
            typeof (VideoCatalogService).Assembly,
            typeof (SearchService).Assembly,
            typeof (SampleDataService).Assembly
        };

        static void Main(string[] args)
        {
            // Configure logging
            Log.Logger = new LoggerConfiguration()
                .WriteTo.ColoredConsole(LogEventLevel.Information, "{Timestamp:HH:mm:ss} [{SourceContext:l}] {Message}{NewLine}{Exception}")
                .CreateLogger();

            // Convert all configurations from our .config file to a Dictionary
            var config = ConfigurationManager.AppSettings.AllKeys.ToDictionary(key => key, key => ConfigurationManager.AppSettings.Get(key));

            // Create IoC container
            IContainer container = CreateContainer();

            // Let the container pick up any components using the MEF-like attributes in referenced assemblies (this will pick up any 
            // exported Grpc server definitions, message bus handlers, and background tasks in the referenced services)
            container = container.WithMefAttributedModel();
            container.RegisterExports(ServiceAssemblies);

            // Get the host and starting port to bind RPC services to
            string host = GetRequiredConfig(config, "ServicesHost");
            string portConfig = GetRequiredConfig(config, "ServicesPort");
            int port = int.Parse(portConfig);

            // Create the server and add the services found in the container
            var server = new Server
            {
                Ports = { new ServerPort(host, port, ServerCredentials.Insecure) }
            };

            // Try to find any Grpc services in the container
            foreach (var grpcService in container.ResolveMany<IGrpcServerService>())
            {
                // Skip any conditional services that shouldn't run in the current environment
                var conditionalService = grpcService as IConditionalGrpcServerService;
                if (conditionalService?.ShouldRun(config) == false)
                    continue;

                // Add to server
                server.Services.Add(grpcService.ToServerServiceDefinition());
            }

            server.Start();
        }

        private static Container CreateContainer()
        {
            var container = new Container();

            // Register Cassandra session factory as singleton
            container.Register(Made.Of(() => CreateCassandraSession()), Reuse.Singleton);
            container.Register<PreparedStatementCache>(Reuse.Singleton);

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
        
        private static IBusServer CreateBusServer()
        {
            return BusBuilder.Configure().WithServiceName("KillrVideo").WithTransport(InMemoryTransport.Instance).Build();
        }

        private static string GetRequiredConfig(IDictionary<string, string> config, string configKey)
        {
            string val;
            if (config.TryGetValue(configKey, out val) == false || string.IsNullOrWhiteSpace(val))
                throw new InvalidOperationException($"You must specify a value for {configKey} in your .config file");

            return val;
        }
    }
}
