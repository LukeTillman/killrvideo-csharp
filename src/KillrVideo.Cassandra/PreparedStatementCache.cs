using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Cassandra;

namespace KillrVideo.Cassandra
{
    /// <summary>
    /// A cache for PreparedStatements based on the CQL string.
    /// </summary>
    public class PreparedStatementCache
    {
        private readonly ISession _session;
        private readonly ConcurrentDictionary<string, Lazy<Task<PreparedStatement>>> _cachedItems;

        /// <summary>
        /// Create a new TaskCache using the provided factoryFunc to generate items from keys.
        /// </summary>
        public PreparedStatementCache(ISession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            _session = session;

            _cachedItems = new ConcurrentDictionary<string, Lazy<Task<PreparedStatement>>>();
        }

        /// <summary>
        /// Gets a PreparedStatement from the cache for the given CQL string, preparing the CQL statement if it isn't in the cache yet.
        /// </summary>
        public ConfiguredTaskAwaitable<PreparedStatement> GetOrAddAsync(string cql)
        {
            return GetOrAdd(cql).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets multiple PreparedStatements from the cache, preparing any CQL statements that aren't in the cache yet.
        /// </summary>
        public ConfiguredTaskAwaitable<PreparedStatement[]> GetOrAddAllAsync(params string[] cql)
        {
            if (cql == null || cql.Length == 0)
                throw new ArgumentException("You must specify at least one CQL string to get.", nameof(cql));

            return Task.WhenAll(cql.Select(GetOrAdd)).ConfigureAwait(false);
        }

        private Task<PreparedStatement> GetOrAdd(string cql)
        {
            return _cachedItems.AddOrUpdate(cql, CreateTask, UpdateTaskIfFaulted).Value;
        }
        
        private Lazy<Task<PreparedStatement>> CreateTask(string cql)
        {
            return new Lazy<Task<PreparedStatement>>(() => _session.PrepareAsync(cql));
        }

        private Lazy<Task<PreparedStatement>> UpdateTaskIfFaulted(string cql, Lazy<Task<PreparedStatement>> currentValue)
        {
            // If the cached task is faulted, create a new one, otherwise just return the current value
            return currentValue.Value.IsFaulted ? CreateTask(cql) : currentValue;
        }
    }
}
