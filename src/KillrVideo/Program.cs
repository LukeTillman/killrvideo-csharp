using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using DryIoc;
using DryIoc.MefAttributedModel;
using KillrVideo.Cassandra;
using KillrVideo.Comments;
using KillrVideo.Configuration;
using KillrVideo.Host.ServiceDiscovery;
using KillrVideo.MessageBus;
using KillrVideo.Protobuf;
using KillrVideo.Ratings;
using KillrVideo.Search;
using KillrVideo.Statistics;
using KillrVideo.SuggestedVideos;
using KillrVideo.Uploads;
using KillrVideo.UserManagement;
using KillrVideo.VideoCatalog;
using RestSharp;
using Serilog;

namespace KillrVideo
{
    /// <summary>
    /// Console application for running/debugging all the KillrVideo backend services in-process together.
    /// </summary>
    class Program
    {
        private static readonly Assembly[] ProjectAssemblies = new[]
        {
            typeof (CommentsService).Assembly,
            typeof (RatingsService).Assembly,
            typeof (StatisticsService).Assembly,
            typeof (SuggestedVideoService).Assembly,
            typeof (UploadsService).Assembly,
            typeof (UserManagementService).Assembly,
            typeof (VideoCatalogService).Assembly,
            typeof (SearchService).Assembly,
            typeof (Host.Host).Assembly,
            typeof (GrpcServerTask).Assembly,
            typeof (PreparedStatementCache).Assembly,
            typeof (Bus).Assembly,
            typeof (Program).Assembly
        };

        static void Main(string[] args)
        {
            // Configure logging
            Log.Logger = new LoggerConfiguration().MinimumLevel.Information()
                .WriteTo.ColoredConsole(outputTemplate: "{Timestamp:HH:mm:ss} [{SourceContext:l}] {Message}{NewLine}{Exception}")
                .CreateLogger();

            // Create IoC container and add commandline args to it
            IContainer container = CreateContainer();
            container.RegisterInstance(new CommandLineArgs(args));

            // Let the container pick up any components using the MEF-like attributes in referenced assemblies (this will pick up any 
            // exported Grpc server definitions, message bus handlers, and background tasks in the referenced services)
            container = container.WithMefAttributedModel();
            container.RegisterExports(ProjectAssemblies);
            
            // Wait for DSE to become available
            var cassandraInit = container.Resolve<BootstrapDataStaxEnterprise>();
            cassandraInit.RegisterDseOnceAvailable(container).Wait();

            // Start host
            var host = container.Resolve<Host.Host>();
            host.Start();

            // Lookup the web UI's address
            var serviceDiscovery = container.Resolve<IFindServices>();
            string webLocation = $"http://{serviceDiscovery.LookupServiceAsync("web").Result.First()}";

            var logger = Log.ForContext<Program>();
            logger.Information("Open {WebAddress} in a web browser to see the UI", webLocation);
            logger.Information("Killrvideo has started. Press Ctrl+C to exit.");

            var autoResetEvent = new AutoResetEvent(false);
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                autoResetEvent.Set();
            };

            // Wait for Ctrl+C
            autoResetEvent.WaitOne();
            
            // Stop everything
            host.Stop();
            container.Dispose();

            logger.Information("KillrVideo has stopped. Press any key to exit.");
            Console.ReadKey();
        }

        private static Container CreateContainer()
        {
            var container = new Container(rules => rules.WithResolveIEnumerableAsLazyEnumerable());
            
            // Register REST client
            container.Register<IRestClient, RestClient>(Made.Of(() => new RestClient()));

            return container;
        }
    }
}
