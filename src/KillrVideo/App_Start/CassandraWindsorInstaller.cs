using System;
using System.Linq;
using System.Net;
using Cassandra;
using Castle.MicroKernel;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using KillrVideo.Utils;
using KillrVideo.Utils.Configuration;
using Serilog;

namespace KillrVideo
{
    /// <summary>
    /// Installs Cassandra components with Castle Windsor.
    /// </summary>
    public class CassandraWindsorInstaller : IWindsorInstaller
    {
        private static readonly ILogger Logger = Log.ForContext<CassandraWindsorInstaller>();

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

            // Start a cluster builder for connecting to Cassandra
            Builder builder = Cluster.Builder();

            // Allow multiple comma delimited locations to be specified in the configuration
            string[] locations = clusterLocation.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim()).ToArray();
            foreach (string location in locations)
            {
                string[] hostAndPort = location.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (hostAndPort.Length == 1)
                {
                    // Just an IP address or host name
                    builder = builder.AddContactPoint(hostAndPort[0]);
                }
                else if (hostAndPort.Length == 2)
                {
                    // IP Address plus host name
                    var ipEndPoint = new IPEndPoint(IPAddress.Parse(hostAndPort[0]), int.Parse(hostAndPort[1]));
                    builder = builder.AddContactPoint(ipEndPoint);
                }
                else
                {
                    throw new InvalidOperationException(string.Format("Unable to parse Cassandra cluster location '{0}' from configuration.", location));
                }
            }
            
            // Use the Cluster builder to create a cluster
            Cluster cluster = builder.Build();

            // Use the cluster to connect a session to the appropriate keyspace
            ISession session;
            try
            {
                session = cluster.Connect(Keyspace);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Exception while connecting to '{Keyspace}' using '{Hosts}'", Keyspace, clusterLocation);
                throw;
            }

            return session;
        }
    }
}
