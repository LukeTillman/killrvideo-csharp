using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Dse;
using Dse.Graph;
using Dse.Auth;
using DryIoc;
using KillrVideo.Host.ServiceDiscovery;
using Serilog;
using Microsoft.Extensions.Configuration;
using KillrVideo.Configuration;
using System.Reflection;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace KillrVideo.Dse
{
    /// <summary>
    /// Bootstrapper for DSE that will register an ISession instance with the container once the keyspace is ready.
    /// </summary>
    [Export(typeof(BootstrapDataStaxEnterprise))]
    public class BootstrapDataStaxEnterprise
    {
        /// <summary>
        /// Logger for the class to write into both files and console
        /// </summary>
        private static readonly ILogger Logger = Log.ForContext(typeof(BootstrapDataStaxEnterprise));

        /// <summary>
        /// Configuration is externalized in 'Configuration' folder in 'HostConfigurationFactory'
        /// </summary>
        private readonly IConfiguration _kvConfig;

        /// <summary>
        /// Accessing Cassandra host and port from ETCD
        /// </summary>
        private readonly IFindServices _serviceDiscovery;

        /// <summary>
        /// Constructor with dependency injection
        /// </summary>
        public BootstrapDataStaxEnterprise(IFindServices serviceDiscovery, IConfiguration killrVideoConfiguration)
        {
            if (serviceDiscovery == null) throw new ArgumentNullException(nameof(serviceDiscovery));
            _serviceDiscovery = serviceDiscovery;

            if (killrVideoConfiguration == null) throw new ArgumentNullException(nameof(killrVideoConfiguration));
            _kvConfig = killrVideoConfiguration;
        }

        /// <summary>
        /// Register the singleton ISession instance with the container once we can connect to the killrvideo schema.
        /// </summary>
        public async Task RegisterDseOnceAvailable(IContainer container)
        {
            IDseSession session = null;
            int attempts = 0;

            Logger.Information("Initializing connection to DSE Cluster...");
            while (session == null)
            {
                try
                {
                    Logger.Information("+ Reading node addresses from ETCD.");
                    IEnumerable<string> hosts = await _serviceDiscovery
                        .LookupServiceAsync(ConfigKeys.EtcdCassandraKey)
                        .ConfigureAwait(false);
                    
                    // Create cluster builder with contact points
                    var builder = DseCluster.Builder().AddContactPoints(hosts.Select(ToIpEndPoint));

                    // Authentication
                    if (!string.IsNullOrEmpty(_kvConfig[ConfigKeys.DseUsername]) && 
                        !string.IsNullOrEmpty(_kvConfig[ConfigKeys.DsePassword])) {
                        Logger.Information("+ Enable Authentication with user {user}", _kvConfig[ConfigKeys.DseUsername]);
                        builder.WithAuthProvider(
                            new DsePlainTextAuthProvider(_kvConfig[ConfigKeys.DseUsername], 
                                                         _kvConfig[ConfigKeys.DsePassword]));
                    } else {
                        Logger.Information("+ No Authentication");
                    }

                    // SSL : To be tested (first try based on documentation)
                    if (Boolean.Parse(_kvConfig[ConfigKeys.DseEnableSSL])) {
                        String certPath     = _kvConfig[ConfigKeys.DseSslCertPath];
                        String certPassword = _kvConfig[ConfigKeys.DseSslCertPassword];
                        if(string.IsNullOrEmpty(certPath)) {
                            throw new ArgumentNullException("Cannot real SSL File " + certPath);
                        }
                        if (string.IsNullOrEmpty(certPath))
                        {
                            throw new ArgumentNullException("Cannot real SSL Certificate password " + certPath);
                        }
                        Logger.Information("+ Setup SSL options with {certPath}", certPath);
                        SSLOptions sslOptions = new SSLOptions();
                        X509Certificate2[] certs = new X509Certificate2[] { new X509Certificate2(readX509Certificate(certPath), certPassword) };
                        sslOptions.SetCertificateCollection(new X509CertificateCollection(certs));
                        sslOptions.SetRemoteCertValidationCallback((a1, a2, a3, a4) => true);
                        //sslOptions.SetHostNameResolver((internalIPAddress) => { return "test_client"; });
                        builder.WithSSL(sslOptions);
                    } else {
                        Logger.Information("+ No SSL");
                    }

                    // Query options
                    var queryOptions = new QueryOptions();
                    queryOptions.SetConsistencyLevel(ConsistencyLevel.LocalQuorum);
                    builder.WithQueryOptions(queryOptions);
                   
                    // Graph Options
                    Logger.Information("+ Graph connection to {graphName}", _kvConfig[ConfigKeys.DseGraphName]);
                    var graphOptions = new GraphOptions().SetName(_kvConfig[ConfigKeys.DseGraphName]);
                    graphOptions.SetReadTimeoutMillis(int.Parse(_kvConfig[ConfigKeys.DseGraphReadTimeout]));
                    graphOptions.SetReadConsistencyLevel(ConsistencyLevel.LocalQuorum);
                    builder.WithGraphOptions(graphOptions);

                    // Cassandra
                    session = builder.Build().Connect(_kvConfig[ConfigKeys.DseKeySpace]);
                    Logger.Information("+ Session established to keyspace {keyspace}", _kvConfig[ConfigKeys.DseKeySpace]);
                }
                catch (Exception e)
                {
                    attempts++;
                    session = null;

                    // Don't log exceptions until we've tried 6 times
                    if (attempts >= int.Parse(_kvConfig[ConfigKeys.MaxRetry]))
                    {
                        Logger.Error(e, 
                                     "Cannot connection to DSE after {max} attempts, exiting", 
                                     _kvConfig[ConfigKeys.MaxRetry]);
                        Environment.Exit(404);
                    }
                }

                if (session != null) continue;

                Logger.Information("+ Attempt #{nb}/{max} failed.. trying in {delay} seconds, waiting Dse to Start",
                                   attempts, 
                                   _kvConfig[ConfigKeys.MaxRetry], 
                                   _kvConfig[ConfigKeys.RetryDelay]);
                await Task.Delay(int.Parse(_kvConfig[ConfigKeys.RetryDelay])).ConfigureAwait(false);
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

        public static byte[] readX509Certificate(string resourceName) {
            using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)) {
                byte[] buffer = new byte[1024];
                using (MemoryStream ms = new MemoryStream())
                {
                    while (true)
                    {
                        int read = s.Read(buffer, 0, buffer.Length);
                        if (read <= 0)
                            return ms.ToArray();
                        ms.Write(buffer, 0, read);
                    }
                }
            }
        }
    }
}
