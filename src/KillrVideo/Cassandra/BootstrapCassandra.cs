using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Cassandra;
using DryIoc;
using Serilog;

namespace KillrVideo.Cassandra
{
    /// <summary>
    /// Bootstrapper for Cassandra that will register an ISession instance with the container once Cassandra is ready.
    /// </summary>
    [Export(typeof(BootstrapCassandra))]
    public class BootstrapCassandra
    {
        private static readonly ILogger Logger = Log.ForContext(typeof(BootstrapCassandra));

        private readonly CassandraSessionFactory _sessionFactory;

        public BootstrapCassandra(CassandraSessionFactory sessionFactory)
        {
            if (sessionFactory == null) throw new ArgumentNullException(nameof(sessionFactory));
            _sessionFactory = sessionFactory;
        }

        public async Task RegisterCassandraOnceAvailable(IContainer container)
        {
            ISession session = null;
            int attempts = 0;

            while (session == null)
            {
                try
                {
                    session = await _sessionFactory.GetSessionAsync("killrvideo").ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    attempts++;
                    session = null;

                    // Don't log exceptions until we've tried 6 times
                    if (attempts >= 6)
                        Logger.Error(e, "Error connecting to killrvideo keyspace in Cassandra");
                }

                if (session != null) continue;

                Logger.Information("Waiting for killrvideo keyspace in Cassandra to be ready");
                await Task.Delay(10000).ConfigureAwait(false);
            }

            container.RegisterInstance(session);
        }
    }
}
