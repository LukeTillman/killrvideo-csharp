using System;
using System.Configuration;
using System.Linq;
using System.Web.Mvc;
using Cassandra;
using Castle.Facilities.Startable;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using KillrVideo.Comments;
using KillrVideo.Ratings;
using KillrVideo.Search;
using KillrVideo.Statistics;
using KillrVideo.SuggestedVideos;
using KillrVideo.Uploads;
using KillrVideo.UserManagement;
using KillrVideo.Utils;
using KillrVideo.Utils.Nimbus;
using KillrVideo.VideoCatalog;
using log4net;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;
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
        
        /// <summary>
        /// Creates the Windsor container and does all necessary registrations for the KillrVideo app.
        /// </summary>
        public static IWindsorContainer CreateContainer()
        {
            var container = new WindsorContainer();
            
            // Do container registrations (these would normally be organized as Windsor installers, but for brevity they are inline here)
            RegisterCassandra(container);
            RegisterServices(container);
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

        private static void RegisterServices(WindsorContainer container)
        {
            // Just use the installers in the services
            container.Install(new CommentsServiceWindsorInstaller(), new RatingsServiceWindsorInstaller(), new SearchServiceWindsorInstaller(),
                              new StatisticsServiceWindsorInstaller(), new SuggestedVideosServiceWindsorInstaller(),
                              new UploadsServiceWindsorInstaller(), new UserManagementServiceWindsorInstaller(),
                              new VideoCatalogServiceWindsorInstaller());
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

            // Create the Nimbus type provider to scan the assemblies from the static NimbusAssemblyConfig class
            var typeProvider = new AssemblyScanningTypeProvider(NimbusAssemblyConfig.AssembliesToScan.Distinct().ToArray());
            container.RegisterNimbus(typeProvider);

            // Get app name and unique name
            string appName = string.Format("{0}KillrVideo.Web", namePrefix);
            string uniqueName = string.Format("{0}{1}", namePrefix, RoleEnvironment.CurrentRoleInstance.Id);

            // Register the bus itself and start it when it's resolved for the first time
            container.Register(
                Component.For<ILogger>().ImplementedBy<NimbusLog4NetLogger>().LifestyleSingleton(),
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