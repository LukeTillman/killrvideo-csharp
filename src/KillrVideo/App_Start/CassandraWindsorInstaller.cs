using System;
using System.Linq;
using Cassandra;
using Castle.MicroKernel;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using KillrVideo.Utils;
using KillrVideo.Utils.Configuration;
using log4net;

namespace KillrVideo
{
    /// <summary>
    /// Installs Cassandra components with Castle Windsor.
    /// </summary>
    public class CassandraWindsorInstaller : IWindsorInstaller
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (CassandraWindsorInstaller));

        private const string ClusterLocationAppSettingsKey = "CassandraClusterLocation";
        private const string Keyspace = "killrvideo";

        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(
                // Register session and a statement cache as singletons
                Component.For<ISession>().UsingFactoryMethod(CreateCassandraSession).LifestyleSingleton(),
                Component.For<TaskCache<string, PreparedStatement>>().ImplementedBy<PreparedStatementCache>().LifestyleSingleton()
            );
        }

        private static ISession CreateCassandraSession(IKernel kernel)
        {
            // Get cluster IP/host and keyspace from .config file
            var configRetriever = kernel.Resolve<IGetEnvironmentConfiguration>();
            string clusterLocation = configRetriever.GetSetting(ClusterLocationAppSettingsKey);
            kernel.ReleaseComponent(configRetriever);

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

            return session;
        }
    }
}
