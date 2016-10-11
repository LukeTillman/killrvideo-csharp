using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Cassandra;
using DryIoc;
using KillrVideo.Host.ServiceDiscovery;
using Serilog;

namespace KillrVideo.Cassandra
{
    /// <summary>
    /// Bootstrapper for DSE that will register an ISession instance with the container once the keyspace is ready.
    /// </summary>
    [Export(typeof(BootstrapDataStaxEnterprise))]
    public class BootstrapDataStaxEnterprise
    {
        private const string Keyspace = "killrvideo";
        private const int AttemptsBeforeLoggingErrors = 6;

        private static readonly ILogger Logger = Log.ForContext(typeof(BootstrapDataStaxEnterprise));

        private readonly IFindServices _serviceDiscovery;

        public BootstrapDataStaxEnterprise(IFindServices serviceDiscovery)
        {
            if (serviceDiscovery == null) throw new ArgumentNullException(nameof(serviceDiscovery));
            _serviceDiscovery = serviceDiscovery;
        }

        /// <summary>
        /// Register the singleton ISession instance with the container once we can connect to the killrvideo schema.
        /// </summary>
        public async Task RegisterDseOnceAvailable(IContainer container)
        {
            ISession session = null;
            int attempts = 0;

            while (session == null)
            {
                try
                {
                    // Find the cassandra service
                    IEnumerable<string> hosts = await _serviceDiscovery.LookupServiceAsync("cassandra").ConfigureAwait(false);
                    IEnumerable<IPEndPoint> hostIpAndPorts = hosts.Select(ToIpEndPoint);

                    // Create cluster builder with contact points
                    var builder = Cluster.Builder().AddContactPoints(hostIpAndPorts);

                    // Set default query options
                    var queryOptions = new QueryOptions();
                    queryOptions.SetConsistencyLevel(ConsistencyLevel.LocalQuorum);
                    builder.WithQueryOptions(queryOptions);

                    Logger.Information("Creating Cassandra session connection to {Keyspace}", Keyspace);
                    session = builder.Build().Connect(Keyspace);
                }
                catch (Exception e)
                {
                    attempts++;
                    session = null;

                    // Don't log exceptions until we've tried 6 times
                    if (attempts >= AttemptsBeforeLoggingErrors)
                        Logger.Error(e, "Error connecting to killrvideo keyspace in Cassandra");
                }

                if (session != null) continue;

                Logger.Information("Waiting for killrvideo keyspace in Cassandra to be ready");
                await Task.Delay(10000).ConfigureAwait(false);
            }

            // Since session objects should be created once and then reused, register the instance with the container
            // which will register it as a singleton by default
            container.RegisterInstance(session);
        }
        
        private static IPEndPoint ToIpEndPoint(string hostAndPort)
        {
            string[] parts = hostAndPort.Split(':');
            if (parts.Length != 2)
                throw new ArgumentException($"{hostAndPort} is not format host:port");

            return new IPEndPoint(IPAddress.Parse(parts[0]), int.Parse(parts[1]));
        }
    }
}
