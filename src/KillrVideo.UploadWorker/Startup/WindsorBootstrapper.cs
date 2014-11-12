using System;
using System.Configuration;
using System.Linq;
using Cassandra;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using KillrVideo.UploadWorker.Jobs;
using log4net;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.MediaServices.Client;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Rebus;
using Rebus.AzureServiceBus;
using Rebus.Castle.Windsor;
using Rebus.Configuration;
using Rebus.Log4Net;

namespace KillrVideo.UploadWorker.Startup
{
    /// <summary>
    /// Bootstrapping class for Castle Windsor IoC container.
    /// </summary>
    public static class WindsorBootstrapper
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (WindsorBootstrapper));

        private const string ClusterLocationAppSettingsKey = "CassandraClusterLocation";
        private const string MediaServicesNameAppSettingsKey = "AzureMediaServicesAccountName";
        private const string MediaServicesKeyAppSettingsKey = "AzureMediaServicesAccountKey";
        private const string StorageConnectionStringAppSettingsKey = "AzureStorageConnectionString";

        private const string Keyspace = "killrvideo";

        /// <summary>
        /// Creates the Windsor container and does all necessary registrations for the KillrVideo.UploadWorker role.
        /// </summary>
        public static IWindsorContainer CreateContainer()
        {
            var container = new WindsorContainer();

            // Do container registrations (these would normally be organized as Windsor installers, but for brevity they are inline here)
            RegisterCassandra(container);
            RegisterDataComponents(container);
            RegisterAzureComponents(container);
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

            // Register both Cluster and ISession instances with Windsor (essentially as Singletons since it will reuse the instance)
            container.Register(
                Component.For<ISession>().Instance(session)
            );
        }

        private static void RegisterDataComponents(WindsorContainer container)
        {
            container.Register(
                // Register all the read/write model objects in the KillrVideo.Data project and register them as Singletons since
                // we want the state in them (reusable prepared statements) to actually be reused.
                Classes.FromAssemblyContaining<VideoLocationType>().Pick()
                       .WithServiceFirstInterface().LifestyleSingleton()
                       .ConfigureFor<LinqUserReadModel>(c => c.IsDefault())     // Change the Type here to use other IUserReadModel implementations (i.e. ADO.NET or core)
            );
        }
        
        private static void RegisterAzureComponents(WindsorContainer container)
        {
            // Get Azure configurations
            string mediaServicesAccountName = GetRequiredSetting(MediaServicesNameAppSettingsKey);
            string mediaServicesAccountKey = GetRequiredSetting(MediaServicesKeyAppSettingsKey);
            string storageConnectionString = GetRequiredSetting(StorageConnectionStringAppSettingsKey);

            var mediaCredentials = new MediaServicesCredentials(mediaServicesAccountName, mediaServicesAccountKey);
            container.Register(
                // Recommended be shared by all CloudMediaContext objects so register as singleton
                Component.For<MediaServicesCredentials>().Instance(mediaCredentials),

                // Not thread-safe, so register as transient
                Component.For<CloudMediaContext>().LifestyleTransient()
            );

            // Setup queue for notifications about video encoding jobs
            var storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            var notificationQueue = storageAccount.CreateCloudQueueClient().GetQueueReference(UploadConfig.NotificationQueueName);
            notificationQueue.CreateIfNotExists();

            container.Register(
                // Register the queue client and get a new one each time (transient) just to be safe
                Component.For<CloudQueueClient>().UsingFactoryMethod(storageAccount.CreateCloudQueueClient).LifestyleTransient()
            );
        }
        
        private static void RegisterMessageBus(WindsorContainer container)
        {
            // Register the bus itself
            IStartableBus startableBus = Configure.With(new WindsorContainerAdapter(container))
                                                  .Logging(l => l.Log4Net())
                                                  .Transport(t => t.UseAzureServiceBus())
                                                  .CreateBus();
            container.Register(Component.For<IStartableBus>().Instance(startableBus));
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
