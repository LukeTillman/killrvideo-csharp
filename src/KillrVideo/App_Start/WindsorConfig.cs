using System;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;
using Cassandra;
using Castle.Facilities.Startable;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using KillrVideo.Comments.Messages.Commands;
using KillrVideo.Comments.ReadModel;
using KillrVideo.Ratings.Messages.Commands;
using KillrVideo.Ratings.ReadModel;
using KillrVideo.Search.ReadModel;
using KillrVideo.Statistics.Messages.Commands;
using KillrVideo.Statistics.ReadModel;
using KillrVideo.SuggestedVideos.ReadModel;
using KillrVideo.Uploads.Messages.RequestResponse;
using KillrVideo.Uploads.ReadModel;
using KillrVideo.UserManagement.Messages.Commands;
using KillrVideo.UserManagement.ReadModel;
using KillrVideo.Utils;
using KillrVideo.VideoCatalog.Messages.Commands;
using KillrVideo.VideoCatalog.ReadModel;
using log4net;
using Microsoft.WindowsAzure;
using Nimbus;
using Nimbus.Configuration;
using Nimbus.Infrastructure;

namespace KillrVideo
{
    /// <summary>
    /// Bootstrapper for the Castle Windsor IoC container.
    /// </summary>
    public static class WindsorConfig
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (WindsorConfig));

        private const string ClusterLocationAppSettingsKey = "CassandraClusterLocation";
        private const string AzureServiceBusConnectionStringKey = "AzureServiceBusConnectionString";
        private const string AzureServiceBusNamePrefixKey = "AzureServiceBusNamePrefix";

        private const string Keyspace = "killrvideo";

        private static readonly Assembly[] ReadModelAssemblies =
        {
            typeof (ICommentReadModel).Assembly, typeof (IRatingsReadModel).Assembly, typeof (ISearchVideosByTag).Assembly,
            typeof (IPlaybackStatsReadModel).Assembly, typeof (ISuggestVideos).Assembly, typeof (IUploadedVideosReadModel).Assembly,
            typeof (IUserReadModel).Assembly, typeof (IVideoCatalogReadModel).Assembly
        };

        private static readonly Assembly[] MessageAssemblies =
        {
            typeof (CommentOnVideo).Assembly, typeof (RateVideo).Assembly, typeof (RecordPlaybackStarted).Assembly,
            typeof (GenerateUploadDestination).Assembly, typeof (CreateUser).Assembly, typeof (SubmitUploadedVideo).Assembly
        };

        /// <summary>
        /// Creates the Windsor container and does all necessary registrations for the KillrVideo app.
        /// </summary>
        public static IWindsorContainer CreateContainer()
        {
            var container = new WindsorContainer();

            // Do container registrations (these would normally be organized as Windsor installers, but for brevity they are inline here)
            RegisterCassandra(container);
            RegisterReadModels(container);
            RegisterMvcControllers(container);
            RegisterMessageBus(container);

            return container;
        }

        private static void RegisterCassandra(WindsorContainer container)
        {
            // Get cluster IP/host and keyspace from .config file
            string clusterLocation = GetRequiredSetting(ClusterLocationAppSettingsKey);

            // Allow multiple comma delimited locations to be specified in the configuration
            string[] locations = clusterLocation.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim()).ToArray();

            // Use the Cluster builder to create a cluster
            Cluster cluster = Cluster.Builder().AddContactPoints(locations).Build();

            // Use the cluster to connect a session to the appropriate keyspace
            ISession session;
            try
            {
                session = cluster.Connect(Keyspace);
            }
            catch (Exception e)
            {
                Logger.Error(string.Format("Exception while connecting to keyspace '{0}' using hosts '{1}'", Keyspace, clusterLocation), e);
                throw;
            }

            // Create a cache for prepared statements that can be used across the app
            var statementCache = new TaskCache<string, PreparedStatement>(cql => session.PrepareAsync(cql));

            // Register ISession instance and a statement cache for reusing prepared statements as singletons
            container.Register(
                Component.For<ISession>().Instance(session),
                Component.For<TaskCache<string, PreparedStatement>>().Instance(statementCache)
            );
        }

        private static void RegisterReadModels(WindsorContainer container)
        {
            // Register the read model classes in all those assemblies
            foreach (var asm in ReadModelAssemblies)
            {
                container.Register(
                    Classes.FromAssembly(asm).Pick()
                           .WithServiceFirstInterface().LifestyleTransient()
                           .ConfigureFor<LinqUserReadModel>(cfg => cfg.IsDefault())
                );
            }
        }

        private static void RegisterMvcControllers(WindsorContainer container)
        {
            // Register all MVC controllers in this assembly with the container
            container.Register(
                Classes.FromThisAssembly().BasedOn<IController>().LifestyleTransient()
            );
        }
        
        private static void RegisterMessageBus(WindsorContainer container)
        {
            // Get the Azure Service Bus connection string and prefix for names
            string connectionString = GetRequiredSetting(AzureServiceBusConnectionStringKey);
            string namePrefix = GetRequiredSetting(AzureServiceBusNamePrefixKey);

            // Create a type provider with all our message assemblies (there shouldn't be any handlers here since the web app only sends commands)
            var typeProvider = new AssemblyScanningTypeProvider(MessageAssemblies);
            container.RegisterNimbus(typeProvider);

            // Get app name and unique name
            string appName = string.Format("{0}KillrVideo.Web", namePrefix);
            string uniqueName = string.Format("{0}{1}", namePrefix, Environment.MachineName);

            // Register the bus itself and start it when it's resolved for the first time
            container.Register(
                Component.For<IBus>()
                         .ImplementedBy<Bus>()
                         .UsingFactoryMethod(
                             () =>
                             new BusBuilder().Configure()
                                             .WithConnectionString(connectionString)
                                             .WithNames(appName, uniqueName)
                                             .WithTypesFrom(typeProvider)
                                             .WithJsonSerializer()
                                             .WithWindsorDefaults(container)
                                             .Build())
                         .LifestyleSingleton()
                         .StartUsingMethod("Start")
                );
        }

        /// <summary>
        /// Gets a required setting from CloudConfigurationManager and throws a ConfigurationErrorsException if setting is null/empty.
        /// </summary>
        private static string GetRequiredSetting(string key)
        {
            var value = CloudConfigurationManager.GetSetting(key);
            if (string.IsNullOrEmpty(value))
                throw new ConfigurationErrorsException(string.Format("No value for required setting {0} in cloud configuration", key));

            return value;
        }
    }
}