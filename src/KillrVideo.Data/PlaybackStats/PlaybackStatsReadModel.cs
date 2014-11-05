using System;
using System.Linq;
using System.Threading.Tasks;
using Cassandra;

namespace KillrVideo.Data.PlaybackStats
{
    /// <summary>
    /// Reads playback stats from Cassandra.
    /// </summary>
    public class PlaybackStatsReadModel : IPlaybackStatsReadModel
    {
        private readonly ISession _session;

        private readonly AsyncLazy<PreparedStatement> _getPlaybacks; 

        public PlaybackStatsReadModel(ISession session)
        {
            if (session == null) throw new ArgumentNullException("session");
            _session = session;

            // Init statements
            _getPlaybacks = new AsyncLazy<PreparedStatement>(() => _session.PrepareAsync("SELECT views FROM video_playback_stats WHERE videoid = ?"));
        }

        /// <summary>
        /// Gets the number of times the specified video has been played.
        /// </summary>
        public async Task<long> GetNumberOfPlays(Guid videoId)
        {
            PreparedStatement prepared = await _getPlaybacks;
            BoundStatement bound = prepared.Bind(videoId);
            RowSet rows = await _session.ExecuteAsync(bound);
            Row row = rows.SingleOrDefault();
            if (row == null)
                return 0;

            return row.GetValue<long>("views");
        }
    }
}