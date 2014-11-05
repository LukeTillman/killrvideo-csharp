using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cassandra;
using Cassandra.Data.Linq;

namespace KillrVideo.Data
{
    public static class HelperExtensions
    {
        /// <summary>
        /// Wraps the BeginPrepare/EndPrepare methods with Task so it can be used with async/await.
        /// </summary>
        public static Task<PreparedStatement> PrepareAsync(this ISession session, string cqlQuery)
        {
            return Task<PreparedStatement>.Factory.FromAsync(session.BeginPrepare, session.EndPrepare, cqlQuery, null);
        }

        /// <summary>
        /// Executes multiple statements async and returns a Task representing the completion of all of them.
        /// </summary>
        public static Task<RowSet[]> ExecuteMultipleAsync(this ISession session, params IStatement[] statements)
        {
            return Task.WhenAll(statements.Select(session.ExecuteAsync));
        }

        /// <summary>
        /// Wraps BeginExecute/EndExecute on a LINQ to CQL CqlQuery&lt;T&gt; object with Task so it can be used with async/await.
        /// </summary>
        public static Task<IEnumerable<T>> ExecuteAsync<T>(this CqlQuery<T> query)
        {
            return Task<IEnumerable<T>>.Factory.FromAsync(query.BeginExecute, query.EndExecute, null);
        }

        /// <summary>
        /// Converts an IEnumerable&lt;T&gt; to a HashSet&lt;T&gt;.
        /// </summary>
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> enumerable)
        {
            return new HashSet<T>(enumerable);
        }

        /// <summary>
        /// Truncates a DateTimeOffset to the specified resolution.  Use the TimeSpan.TicksPerXXX constants for
        /// the resolution parameter.  Returns a new DateTimeOffset.
        /// </summary>
        public static DateTimeOffset Truncate(this DateTimeOffset dateTimeOffset, long resolution)
        {
            return new DateTimeOffset(dateTimeOffset.Ticks - (dateTimeOffset.Ticks % resolution), dateTimeOffset.Offset);
        }
    }
}
