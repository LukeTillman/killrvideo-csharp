using System;
using System.Linq;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.Uploads.ReadModel.Dtos;
using KillrVideo.Utils;

namespace KillrVideo.Uploads.ReadModel
{
    /// <summary>
    /// Reads data from Cassandra for uploaded videos.
    /// </summary>
    public class UploadedVideosReadModel : IUploadedVideosReadModel
    {
        private readonly ISession _session;
        private readonly TaskCache<string, PreparedStatement> _statementCache;

        public UploadedVideosReadModel(ISession session, TaskCache<string, PreparedStatement> statementCache)
        {
            if (session == null) throw new ArgumentNullException("session");
            if (statementCache == null) throw new ArgumentNullException("statementCache");
            _session = session;
            _statementCache = statementCache;
        }

        public async Task<EncodingJobProgress> GetStatusForVideo(Guid videoId)
        {
            PreparedStatement preparedStatement =
                await _statementCache.NoContext.GetOrAddAsync("SELECT newstate, status_date FROM encoding_job_notifications WHERE videoid = ? LIMIT 1");
            RowSet rows = await _session.ExecuteAsync(preparedStatement.Bind(videoId));
            return MapRowToEncodingJobProgress(rows.SingleOrDefault());
        }

        private static EncodingJobProgress MapRowToEncodingJobProgress(Row row)
        {
            if (row == null) return null;

            return new EncodingJobProgress
            {
                CurrentState = row.GetValue<string>("newstate"),
                StatusDate = row.GetValue<DateTimeOffset>("status_date")
            };
        }
    }
}