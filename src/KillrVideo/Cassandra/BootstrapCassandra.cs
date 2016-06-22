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
                    await CreateKeyspaceIfNotExists().ConfigureAwait(false);
                    session = await _sessionFactory.GetSessionAsync("killrvideo").ConfigureAwait(false);
                    await CreateSchemaIfNotExists(session);
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
        
        private async Task CreateKeyspaceIfNotExists()
        {
            ISession session = await _sessionFactory.GetSessionAsync(string.Empty).ConfigureAwait(false);
            session.CreateKeyspaceIfNotExists("killrvideo", new Dictionary<string, string> { { "class", "SimpleStrategy" }, { "replication_factor", "1" } });
        }

        private async Task CreateSchemaIfNotExists(ISession session)
        {
            // Read all statements from the schema.cql file
            var statements = new List<string>();
            var schemaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "schema.cql");
            using (var stream = File.OpenRead(schemaPath))
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                var statement = new StringBuilder();

                while (reader.EndOfStream == false)
                {
                    string line = reader.ReadLine();

                    // Skip blank lines
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    // Skip comments
                    if (line.StartsWith("//"))
                        continue;

                    // Add to current statement
                    statement.AppendLine(line);

                    // Is this the end of a statement?
                    if (line.TrimEnd().EndsWith(";"))
                    {
                        statements.Add(statement.ToString());
                        statement.Clear();
                    }
                }
            }

            // Execute all the statements from the file in order
            foreach (string statement in statements)
            {
                await session.ExecuteAsync(new SimpleStatement(statement)).ConfigureAwait(false);
            }
        }
    }
}
