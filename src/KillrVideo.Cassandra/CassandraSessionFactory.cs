using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.Host.ServiceDiscovery;
using Serilog;

namespace KillrVideo.Cassandra
{
    /// <summary>
    /// A factory that can create a Cassandra ISession instances and caches them so there is only one per keyspace.
    /// </summary>
    [Export(typeof(CassandraSessionFactory))]
    public class CassandraSessionFactory
    {
        private const string NoKeyspace = "NO_KEYSPACE";

        private static readonly ILogger Logger = Log.ForContext(typeof (CassandraSessionFactory));

        private readonly ConcurrentDictionary<string, Lazy<Task<ISession>>> _sessionCache =
            new ConcurrentDictionary<string, Lazy<Task<ISession>>>(StringComparer.OrdinalIgnoreCase);

        private readonly IFindServices _serviceDiscovery;

        public CassandraSessionFactory(IFindServices serviceDiscovery)
        {
            if (serviceDiscovery == null) throw new ArgumentNullException(nameof(serviceDiscovery));
            _serviceDiscovery = serviceDiscovery;
        }
        
        public Task<ISession> GetSessionAsync(string keyspace)
        {
            if (string.IsNullOrEmpty(keyspace))
                keyspace = NoKeyspace;

            return _sessionCache.AddOrUpdate(keyspace, CreateTask, UpdateTaskIfFaulted).Value;
        }

        private Lazy<Task<ISession>> CreateTask(string keyspace)
        {
            return new Lazy<Task<ISession>>(() => CreateSession(keyspace));
        }

        private Lazy<Task<ISession>> UpdateTaskIfFaulted(string keyspace, Lazy<Task<ISession>> currentValue)
        {
            return currentValue.Value.IsFaulted ? CreateTask(keyspace) : currentValue;
        }

        private async Task<ISession> CreateSession(string keyspace)
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

            Logger.Information("Creating Cassandra session connection to {Keyspace}", keyspace);
            return keyspace == NoKeyspace
                ? builder.Build().Connect()
                : builder.Build().Connect(keyspace);
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
