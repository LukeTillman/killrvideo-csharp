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
using KillrVideo.Host.Config;
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
            typeof (SampleDataService).Assembly,
            typeof (AppSettingsConfiguration).Assembly,
            typeof (GrpcServerTask).Assembly,
            typeof (CassandraSessionFactory).Assembly,
            typeof (Bus).Assembly,
            typeof (Program).Assembly
        };

        static void Main(string[] args)
        {
            // Configure logging
            Log.Logger = new LoggerConfiguration().MinimumLevel.Debug()
                .WriteTo.ColoredConsole(outputTemplate: "{Timestamp:HH:mm:ss} [{SourceContext:l}] {Message}{NewLine}{Exception}")
                .CreateLogger();

            // Create IoC container
            IContainer container = CreateContainer();

            // Let the container pick up any components using the MEF-like attributes in referenced assemblies (this will pick up any 
            // exported Grpc server definitions, message bus handlers, and background tasks in the referenced services)
            container = container.WithMefAttributedModel();
            container.RegisterExports(ProjectAssemblies);

            // Start host
            var host = container.Resolve<Host.Host>();
            host.Start();

            Console.ReadKey();
            host.Stop();

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
