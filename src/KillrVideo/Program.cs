using System;
using System.Configuration;
using System.Net;
using Cassandra;
using KillrVideo.Comments;
using KillrVideo.MessageBus.Transport;

namespace KillrVideo
{
    /// <summary>
    /// Console application for running/debugging all the KillrVideo backend services in-process together.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            // Get the host and starting port to bind RPC services to
            string host = ConfigurationManager.AppSettings.Get("ServicesHost");
            if (string.IsNullOrWhiteSpace(host))
                throw new InvalidOperationException("You must specify the ServicesHost configuration option");

            string portConfig = ConfigurationManager.AppSettings.Get("ServicesStartingPort");
            if (string.IsNullOrWhiteSpace(portConfig))
                throw new InvalidOperationException("You must specify the ServicesStartPort configuration option");

            int port = int.Parse(portConfig);
            
            // Setup a C* session to KillrVideo that everyone can share
            ISession cassandra = CreateCassandraSession();
           
            // Start comments service
            var commentsRpc = new CommentsRpcServer();
            commentsRpc.Start(new CommentsRpcServerConfig
            {
                BusTransport = InMemoryTransport.Instance,
                Cassandra = cassandra,
                Host = "localhost",
                Port = port++
            });
        }

        private static ISession CreateCassandraSession()
        {
            string[] hosts = ConfigurationManager.AppSettings.Get("CassandraHosts").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (hosts.Length == 0)
                throw new InvalidOperationException("You must specify the CassandraHosts configuration option");

            Builder builder = Cluster.Builder();

            // Allow for multiple hosts
            foreach (string host in hosts)
            {
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

            return builder.Build().Connect("killrvideo");
        }
    }
}
