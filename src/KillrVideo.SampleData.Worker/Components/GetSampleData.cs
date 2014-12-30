using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.Utils;

namespace KillrVideo.SampleData.Worker.Components
{
    /// <summary>
    /// Gets sample data from Cassandra.
    /// </summary>
    public class GetSampleData : IGetSampleData
    {
        private readonly ISession _session;
        private readonly TaskCache<string, PreparedStatement> _statementCache;

        public GetSampleData(ISession session, TaskCache<string, PreparedStatement> statementCache)
        {
            if (session == null) throw new ArgumentNullException("session");
            if (statementCache == null) throw new ArgumentNullException("statementCache");
            _session = session;
            _statementCache = statementCache;
        }

        /// <summary>
        /// Gets a random collection of sample user ids.
        /// </summary>
        public async Task<IEnumerable<Guid>> GetRandomSampleUserIds(int maxCount)
        {
            // Use the token() function to get the first maxCount ids above an below a random user Id
            PreparedStatement[] prepared = await _statementCache.NoContext.GetOrAddAllAsync(
                "SELECT userid FROM sample_data_users WHERE token(userid) >= token(?) LIMIT ?",
                "SELECT userid FROM sample_data_users WHERE token(userid) < token(?) LIMIT ?");

            var randomId = Guid.NewGuid();
            var execTasks = new Task<RowSet>[2];

            // Execute in parallel
            execTasks[0] = _session.ExecuteAsync(prepared[0].Bind(randomId, maxCount));
            execTasks[1] = _session.ExecuteAsync(prepared[1].Bind(randomId, maxCount));

            RowSet[] results = await Task.WhenAll(execTasks).ConfigureAwait(false);
            
            // Union the results together and take the first maxCount available
            return results.SelectMany(rowset => rowset).Select(r => r.GetValue<Guid>("userid")).Take(maxCount).ToList();
        }
    }
}