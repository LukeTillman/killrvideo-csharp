using System;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.Utils;
using KillrVideo.VideoCatalog.Messages.Events;
using Nimbus.Handlers;

namespace KillrVideo.SampleData.Worker.Handlers
{
    /// <summary>
    /// Records the video id of any videos added to the site so that they can potentially be used when adding
    /// video-related sample data like comments, ratings, etc.
    /// </summary>
    public class RecordVideosAddedHandler : IHandleCompetingEvent<IVideoAdded>
    {
        private readonly ISession _session;
        private readonly TaskCache<string, PreparedStatement> _statementCache;

        public RecordVideosAddedHandler(ISession session, TaskCache<string, PreparedStatement> statementCache)
        {
            if (session == null) throw new ArgumentNullException("session");
            if (statementCache == null) throw new ArgumentNullException("statementCache");
            _session = session;
            _statementCache = statementCache;
        }

        public async Task Handle(IVideoAdded busEvent)
        {
            // Record the id in our sample data tracking table
            PreparedStatement prepared = await _statementCache.NoContext.GetOrAddAsync("INSERT INTO sample_data_videos (videoid) VALUES (?)");
            await _session.ExecuteAsync(prepared.Bind(busEvent.VideoId));
        }
    }
}
