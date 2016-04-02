using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using Cassandra;
using Grpc.Core;
using KillrVideo.Cassandra;
using KillrVideo.Protobuf;
using KillrVideo.Protobuf.Services;

namespace KillrVideo.Statistics
{
    /// <summary>
    /// An implementation of the video statistics service that stores video stats in Cassandra.
    /// </summary>
    [Export(typeof(IGrpcServerService))]
    public class StatisticsServiceImpl : StatisticsService.IStatisticsService, IGrpcServerService
    {
        private readonly ISession _session;
        private readonly PreparedStatementCache _statementCache;
        
        public StatisticsServiceImpl(ISession session, PreparedStatementCache statementCache)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (statementCache == null) throw new ArgumentNullException(nameof(statementCache));
            _session = session;
            _statementCache = statementCache;
        }

        /// <summary>
        /// Convert this instance to a ServerServiceDefinition that can be run on a Grpc server.
        /// </summary>
        public ServerServiceDefinition ToServerServiceDefinition()
        {
            return StatisticsService.BindService(this);
        }

        /// <summary>
        /// Records that playback has been started for a video.
        /// </summary>
        public async Task<RecordPlaybackStartedResponse> RecordPlaybackStarted(RecordPlaybackStartedRequest request, ServerCallContext context)
        {
            PreparedStatement prepared = await _statementCache.GetOrAddAsync("UPDATE video_playback_stats SET views = views + 1 WHERE videoid = ?");
            BoundStatement bound = prepared.Bind(request.VideoId.ToGuid());
            await _session.ExecuteAsync(bound).ConfigureAwait(false);
            return new RecordPlaybackStartedResponse();
        }

        /// <summary>
        /// Gets the number of times the specified videos have been played.
        /// </summary>
        public async Task<GetNumberOfPlaysResponse> GetNumberOfPlays(GetNumberOfPlaysRequest request, ServerCallContext context)
        {
            // Enforce some sanity on this until we can change the data model to avoid the multi-get
            if (request.VideoIds.Count > 20) throw new ArgumentOutOfRangeException(nameof(request.VideoIds), "Cannot do multi-get on more than 20 video id keys.");

            PreparedStatement prepared = await _statementCache.GetOrAddAsync("SELECT videoid, views FROM video_playback_stats WHERE videoid = ?"); ;

            // Run queries in parallel (another example of multi-get at the driver level)
            var idsAndTasks = request.VideoIds.Select(id => new
            {
                VideoId = id,
                ExecuteTask = _session.ExecuteAsync(prepared.Bind(id.ToGuid()))
            }).ToArray();
            await Task.WhenAll(idsAndTasks.Select(idAndResult => idAndResult.ExecuteTask)).ConfigureAwait(false);

            // Be sure to return stats for each video id (even if the row was null)
            var response = new GetNumberOfPlaysResponse();
            response.Stats.Add(idsAndTasks.Select(idTask => new PlayStats
            {
                VideoId = idTask.VideoId,

                // Return 0 views when a video id doesn't return any rows
                Views = idTask.ExecuteTask.Result.SingleOrDefault()?.GetValue<long>("views") ?? 0
            }));

            return response;
        }
    }
}