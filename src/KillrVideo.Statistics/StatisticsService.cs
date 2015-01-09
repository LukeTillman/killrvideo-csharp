using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.Statistics.Dtos;
using KillrVideo.Utils;

namespace KillrVideo.Statistics
{
    /// <summary>
    /// An implementation of the video statistics service that stores video stats in Cassandra.
    /// </summary>
    public class StatisticsService : IStatisticsService
    {
        private readonly ISession _session;
        private readonly TaskCache<string, PreparedStatement> _statementCache;
        
        public StatisticsService(ISession session, TaskCache<string, PreparedStatement> statementCache)
        {
            if (session == null) throw new ArgumentNullException("session");
            if (statementCache == null) throw new ArgumentNullException("statementCache");
            _session = session;
            _statementCache = statementCache;
        }

        /// <summary>
        /// Records that playback has been started for a video.
        /// </summary>
        public async Task RecordPlaybackStarted(RecordPlaybackStarted playback)
        {
            PreparedStatement prepared = await _statementCache.NoContext.GetOrAddAsync("UPDATE video_playback_stats SET views = views + 1 WHERE videoid = ?");
            BoundStatement bound = prepared.Bind(playback.VideoId);
            await _session.ExecuteAsync(bound).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the number of times the specified video has been played.
        /// </summary>
        public async Task<PlayStats> GetNumberOfPlays(Guid videoId)
        {
            PreparedStatement prepared = await _statementCache.NoContext.GetOrAddAsync("SELECT videoid, views FROM video_playback_stats WHERE videoid = ?");
            BoundStatement bound = prepared.Bind(videoId);
            RowSet rows = await _session.ExecuteAsync(bound).ConfigureAwait(false);
            return MapRowToPlayStats(rows.SingleOrDefault(), videoId);
        }

        /// <summary>
        /// Gets the number of times the specified videos have been played.
        /// </summary>
        public async Task<IEnumerable<PlayStats>> GetNumberOfPlays(ISet<Guid> videoIds)
        {
            // Enforce some sanity on this until we can change the data model to avoid the multi-get
            if (videoIds.Count > 20) throw new ArgumentOutOfRangeException("videoIds", "Cannot do multi-get on more than 20 video id keys.");

            var prepared = await _statementCache.NoContext.GetOrAddAsync("SELECT videoid, views FROM video_playback_stats WHERE videoid = ?"); ;

            // Run queries in parallel (another example of multi-get at the driver level)
            var idsAndTasks = videoIds.Select(id => new { VideoId = id, ExecuteTask = _session.ExecuteAsync(prepared.Bind(id)) }).ToArray();
            await Task.WhenAll(idsAndTasks.Select(idAndResult => idAndResult.ExecuteTask)).ConfigureAwait(false);

            // Be sure to return stats for each video id (even if the row was null)
            return idsAndTasks.Select(idTask => MapRowToPlayStats(idTask.ExecuteTask.Result.SingleOrDefault(), idTask.VideoId));
        }

        private static PlayStats MapRowToPlayStats(Row row, Guid videoId)
        {
            // For null rows, just return an empty stats object with 0 views (since we won't have rows for a video until it has at least one view)
            if (row == null)
                return new PlayStats { VideoId = videoId, Views = 0 };

            return new PlayStats
            {
                VideoId = row.GetValue<Guid>("videoid"),
                Views = row.GetValue<long>("views")
            };
        }
    }
}