using System;
using System.Linq;
using System.Web.Mvc;
using Cassandra;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using KillrVideo.Data;
using KillrVideo.Data.Users;
using KillrVideo.Utils;
using log4net;
using Microsoft.WindowsAzure.MediaServices.Client;

namespace KillrVideo
{
    /// <summary>
    /// Bootstrapper for the Castle Windsor IoC container.
    /// </summary>
    public static class WindsorConfig
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (WindsorConfig));

        private const string ClusterLocationAppSettingsKey = "CassandraClusterLocation";
        private const string MediaServicesNameAppSettingsKey = "AzureMediaServicesAccountName";
        private const string MediaServicesKeyAppSettingsKey = "AzureMediaServicesAccountKey";
        private const string StorageConnectionStringAppSettingsKey = "AzureStorageConnectionString";

        private const string Keyspace = "killrvideo";

        /// <summary>
        /// Creates the Windsor container and does all necessary registrations for the KillrVideo app.
        /// </summary>
        public static IWindsorContainer CreateContainer()
        {
            var container = new WindsorContainer();

            // Do container registrations (these would normally be organized as Windsor installers, but for brevity they are inline here)
            RegisterCassandra(container);
            RegisterDataComponents(container);
            RegisterMvcControllers(container);
            RegisterAzureComponents(container);

            return container;
        }

        private static void RegisterCassandra(WindsorContainer container)
        {
            // Get cluster IP/host and keyspace from .config file
            string clusterLocation = ConfigHelper.GetRequiredSetting(ClusterLocationAppSettingsKey);

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

            // Register both Cluster and ISession instances with Windsor (essentially as Singletons since it will reuse the instance)
            container.Register(
                Component.For<Cluster>().Instance(cluster),
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

        private static void RegisterMvcControllers(WindsorContainer container)
        {
            // Register all MVC controllers in this assembly with the container
            container.Register(
                Classes.FromThisAssembly().BasedOn<IController>().LifestyleTransient()
            );
        }

        private static void RegisterAzureComponents(WindsorContainer container)
        {
            // If any of the Azure configs are empty, assume we're going to run without upload capability (TODO: Create a no-op media services component?)
            string mediaServicesAccountName = ConfigHelper.GetOptionalSetting(MediaServicesNameAppSettingsKey);
            if (string.IsNullOrEmpty(mediaServicesAccountName))
            {
                Logger.WarnFormat("No media services account name found under key {0}, upload will be disabled.", MediaServicesNameAppSettingsKey);
                return;
            }

            string mediaServicesAccountKey = ConfigHelper.GetOptionalSetting(MediaServicesKeyAppSettingsKey);
            if (string.IsNullOrEmpty(mediaServicesAccountKey))
            {
                Logger.WarnFormat("No media services account key found under key {0}, upload will be disabled.", MediaServicesKeyAppSettingsKey);
                return;
            }

            string storageConnectionString = ConfigHelper.GetOptionalSetting(StorageConnectionStringAppSettingsKey);
            if (string.IsNullOrEmpty(storageConnectionString))
            {
                Logger.WarnFormat("No storage account connection string found under key {0}, upload will be disabled.",
                                  StorageConnectionStringAppSettingsKey);
                return;
            }
            
            // We've got all the configs we need
            var mediaCredentials = new MediaServicesCredentials(mediaServicesAccountName, mediaServicesAccountKey);
            container.Register(
                Component.For<MediaServicesCredentials>().Instance(mediaCredentials),
                Component.For<CloudMediaContext>().LifestyleTransient()
            );
        }
    }
}