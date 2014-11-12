using System;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.Utils;

namespace KillrVideo.Statistics
{
    /// <summary>
    /// Records playback stats in Cassandra.
    /// </summary>
    public class PlaybackStatsWriteModel : IPlaybackStatsWriteModel
    {
        private readonly ISession _session;

        private readonly AsyncLazy<PreparedStatement> _recordPlaybackStarted; 

        public PlaybackStatsWriteModel(ISession session)
        {
            if (session == null) throw new ArgumentNullException("session");
            _session = session;

            // Init statements
            _recordPlaybackStarted = new AsyncLazy<PreparedStatement>(() => _session.PrepareAsync(
                "UPDATE video_playback_stats SET views = views + 1 WHERE videoid = ?"));
        }

        /// <summary>
        /// Records that video playback was started for the given video Id.
        /// </summary>
        public async Task RecordPlaybackStarted(Guid videoId)
        {
            PreparedStatement prepared = await _recordPlaybackStarted;
            BoundStatement bound = prepared.Bind(videoId);
            await _session.ExecuteAsync(bound);
        }
    }
}