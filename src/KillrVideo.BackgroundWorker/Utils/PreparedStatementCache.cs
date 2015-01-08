using System;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.Utils;

namespace KillrVideo.BackgroundWorker.Utils
{
    /// <summary>
    /// A cache for PreparedStatement instances that can be reused across the application.
    /// </summary>
    public class PreparedStatementCache : TaskCache<string, PreparedStatement>
    {
        private readonly ISession _session;

        public PreparedStatementCache(ISession session)
        {
            if (session == null) throw new ArgumentNullException("session");
            _session = session;
        }

        protected override Task<PreparedStatement> CreateTaskForKey(string key)
        {
            return _session.PrepareAsync(key);
        }
    }
}
