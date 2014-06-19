using System.Configuration;
using System.Web.Mvc;
using Cassandra;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using KillrVideo.Data;
using KillrVideo.Data.Users;

namespace KillrVideo
{
    /// <summary>
    /// Bootstrapper for the Castle Windsor IoC container.
    /// </summary>
    public static class WindsorConfig
    {
        private const string ClusterLocationAppSettingsKey = "CassandraClusterLocation";
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

            return container;
        }

        private static void RegisterCassandra(WindsorContainer container)
        {
            // Get cluster IP/host and keyspace from .config file
            string clusterLocation = ConfigurationManager.AppSettings[ClusterLocationAppSettingsKey];

            // Use the Cluster builder to create a cluster
            Cluster cluster = Cluster.Builder().AddContactPoint(clusterLocation).Build();

            // Use the cluster to connect a session to the appropriate keyspace
            ISession session = cluster.Connect(Keyspace);

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
    }
}