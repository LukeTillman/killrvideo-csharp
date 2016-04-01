using System;
using System.ComponentModel.Composition;
using System.Net;
using Cassandra;
using DryIocAttributes;
using KillrVideo.Host.Config;
using Serilog;

namespace KillrVideo.Cassandra
{
    /// <summary>
    /// A factory that can create a Cassandra ISession instance based on the host configuration.
    /// </summary>
    [Export, AsFactory]
    public static class CassandraSessionFactory
    {
        private static readonly ILogger Logger = Log.ForContext(typeof (CassandraSessionFactory));

        public const string CassandraHostsConfigKey = "CassandraHosts";

        [Export]
        public static ISession CreateSession(IHostConfiguration config)
        {
            string[] hosts = config.GetRequiredConfigurationValue(CassandraHostsConfigKey).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (hosts.Length == 0)
                throw new InvalidOperationException("You must specify the CassandraHosts configuration option");

            Builder builder = Cluster.Builder();

            // Allow for multiple hosts
            foreach (string host in hosts)
            {
                Logger.Debug("Adding Cassandra contact point {CassandraHost}", host);

                string[] hostAndPort = host.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                switch (hostAndPort.Length)
                {
                    case 1:
                        builder.AddContactPoint(hostAndPort[0]);
                        break;
                    case 2:
                        builder.AddContactPoint(new IPEndPoint(IPAddress.Parse(hostAndPort[0]), int.Parse(hostAndPort[1])));
                        break;
                    default:
                        throw new InvalidOperationException($"Unable to parse host {host} from CassandraHosts configuration option");
                }
            }

            Logger.Information("Creating Cassandra session connection to killrvideo schema");
            return builder.Build().Connect("killrvideo");
        }
    }
}
