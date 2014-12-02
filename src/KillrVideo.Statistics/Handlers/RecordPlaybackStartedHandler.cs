using System;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.Statistics.Messages.Commands;
using KillrVideo.Utils;
using Nimbus.Handlers;

namespace KillrVideo.Statistics.Handlers
{
    /// <summary>
    /// Records video playback statistics.
    /// </summary>
    public class RecordPlaybackStartedHandler : IHandleCommand<RecordPlaybackStarted>
    {
        private readonly ISession _session;
        private readonly TaskCache<string, PreparedStatement> _statementCache;
        
        public RecordPlaybackStartedHandler(ISession session, TaskCache<string, PreparedStatement> statementCache)
        {
            if (session == null) throw new ArgumentNullException("session");
            if (statementCache == null) throw new ArgumentNullException("statementCache");
            _session = session;
            _statementCache = statementCache;
        }

        public async Task Handle(RecordPlaybackStarted playback)
        {
            PreparedStatement prepared = await _statementCache.NoContext.GetOrAddAsync("UPDATE video_playback_stats SET views = views + 1 WHERE videoid = ?");
            BoundStatement bound = prepared.Bind(playback.VideoId);
            await _session.ExecuteAsync(bound);
        }
    }
}
