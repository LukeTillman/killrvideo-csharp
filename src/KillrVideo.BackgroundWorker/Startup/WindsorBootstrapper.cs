using System;
using System.Configuration;
using System.Linq;
using System.Reflection;
using Cassandra;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using KillrVideo.SampleData.Worker;
using KillrVideo.Search.Worker;
using KillrVideo.Uploads.Worker;
using KillrVideo.Utils;
using KillrVideo.Utils.Nimbus;
using KillrVideo.VideoCatalog.Worker;
using log4net;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;
using Nimbus;
using Nimbus.Configuration;
using Nimbus.Infrastructure;

namespace KillrVideo.BackgroundWorker.Startup
{
    /// <summary>
    /// Bootstrapping class for Castle Windsor IoC container.
    /// </summary>
    public static class WindsorBootstrapper
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (WindsorBootstrapper));

        private const string ClusterLocationAppSettingsKey = "CassandraClusterLocation";
        private const string Keyspace = "killrvideo";

        private const string AzureServiceBusConnectionStringKey = "AzureServiceBusConnectionString";
        private const string AzureServiceBusNamePrefixKey = "AzureServiceBusNamePrefix";

        private const string YouTubeApiKey = "YouTubeApiKey";

        /// <summary>
        /// Creates the Windsor container and does all necessary registrations for the KillrVideo.UploadWorker role.
        /// </summary>
        public static IWindsorContainer CreateContainer()
        {
            var container = new WindsorContainer();

            string youTubeApiKey = GetRequiredSetting(YouTubeApiKey);

            // Install all the components from the workers we're composing here in this endpoint
            container.Install(new SearchWorkerWindsorInstaller(), new UploadsWorkerWindsorInstaller(), new VideoCatalogWorkerWindsorInstaller(),
                              new SampleDataWorkerWindsorInstaller(RoleEnvironment.CurrentRoleInstance.Id, youTubeApiKey));

            // Do container registrations (these would normally be organized as Windsor installers, but for brevity they are inline here)
            RegisterCassandra(container);
            RegisterMessageBus(container);

            return container;
        }

        private static void RegisterCassandra(WindsorContainer container)
        {
            // Get cluster IP/host and keyspace from .config file
            string clusterLocation = GetRequiredSetting(ClusterLocationAppSettingsKey);

            // Allow multiple comma delimited locations to be specified in the configuration
            string[] locations = clusterLocation.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim()).ToArray();

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
        
        private static void RegisterMessageBus(WindsorContainer container)
        {
            // Get the Azure Service Bus connection string and prefix for names
            string connectionString = GetRequiredSetting(AzureServiceBusConnectionStringKey);
            string namePrefix = GetRequiredSetting(AzureServiceBusNamePrefixKey);

            // Ask the container for any assembly config that's been registered
            NimbusAssemblyConfig[] nimbusAssemblyConfigs = container.ResolveAll<NimbusAssemblyConfig>();
            Assembly[] assemblies = nimbusAssemblyConfigs.SelectMany(ac => ac.AssembliesToScan).Distinct().ToArray();
            
            // Create the Nimbus type provider to scan those assemblies and register with container
            var typeProvider = new AssemblyScanningTypeProvider(assemblies);
            container.RegisterNimbus(typeProvider);

            // Get app name and unique name
            string appName = string.Format("{0}KillrVideo.BackgroundWorker", namePrefix);
            string uniqueName = string.Format("{0}{1}", namePrefix, RoleEnvironment.CurrentRoleInstance.Id);

            // Register the bus itself
            container.Register(
                Component.For<IBus, Bus>()
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
