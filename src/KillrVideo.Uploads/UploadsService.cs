using System;
using System.Linq;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.Uploads.Dtos;
using KillrVideo.Utils;
using Nimbus;

namespace KillrVideo.Uploads
{
    /// <summary>
    /// An implementation of the video uploads service that stores data in Cassandra and uses a message bus for sending commands to
    /// backend processes and publishing events.
    /// </summary>
    public class UploadsService : IUploadsService
    {
        private readonly ISession _session;
        private readonly TaskCache<string, PreparedStatement> _statementCache;
        private readonly IBus _bus;

        public UploadsService(ISession session, TaskCache<string, PreparedStatement> statementCache, IBus bus)
        {
            if (session == null) throw new ArgumentNullException("session");
            if (statementCache == null) throw new ArgumentNullException("statementCache");
            if (bus == null) throw new ArgumentNullException("bus");
            _session = session;
            _statementCache = statementCache;
            _bus = bus;
        }

        /// <summary>
        /// Generates upload destinations for users to upload videos.
        /// </summary>
        public Task<UploadDestination> GenerateUploadDestination(GenerateUploadDestination uploadDestination)
        {
            // Do request/response via the bus
            return _bus.Request(uploadDestination);
        }

        /// <summary>
        /// Marks an upload as complete.
        /// </summary>
        public Task MarkUploadComplete(MarkUploadComplete upload)
        {
            // Send command via the bus
            return _bus.Send(upload);
        }

        /// <summary>
        /// Gets the status of an uploaded video's encoding job by the video Id.
        /// </summary>
        public async Task<EncodingJobProgress> GetStatusForVideo(Guid videoId)
        {
            PreparedStatement preparedStatement =
                await _statementCache.NoContext.GetOrAddAsync("SELECT newstate, status_date FROM encoding_job_notifications WHERE videoid = ? LIMIT 1");
            RowSet rows = await _session.ExecuteAsync(preparedStatement.Bind(videoId)).ConfigureAwait(false);
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