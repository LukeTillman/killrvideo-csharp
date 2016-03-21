using System;
using System.Linq;
using System.Threading.Tasks;
using Cassandra;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using KillrVideo.Utils;
using Nimbus;

namespace KillrVideo.Uploads
{
    /// <summary>
    /// An implementation of the video uploads service that stores data in Cassandra and uses a message bus for sending commands to
    /// backend processes and publishing events.
    /// </summary>
    public class UploadsServiceImpl : UploadsService.IUploadsService
    {
        private readonly ISession _session;
        private readonly TaskCache<string, PreparedStatement> _statementCache;
        private readonly IBus _bus;

        public UploadsServiceImpl(ISession session, TaskCache<string, PreparedStatement> statementCache, IBus bus)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (statementCache == null) throw new ArgumentNullException(nameof(statementCache));
            if (bus == null) throw new ArgumentNullException(nameof(bus));
            _session = session;
            _statementCache = statementCache;
            _bus = bus;
        }

        /// <summary>
        /// Generates upload destinations for users to upload videos.
        /// </summary>
        public Task<GetUploadDestinationResponse> GetUploadDestination(GetUploadDestinationRequest request, ServerCallContext context)
        {
            // Do request/response via the bus
            return _bus.Request(request);
        }

        /// <summary>
        /// Marks an upload as complete.
        /// </summary>
        public Task<MarkUploadCompleteResponse> MarkUploadComplete(MarkUploadCompleteRequest request, ServerCallContext context)
        {
            // Send command via the bus
            return _bus.Send(request);
        }

        /// <summary>
        /// Gets the status of an uploaded video's encoding job by the video Id.
        /// </summary>
        public async Task<GetStatusOfVideoResponse> GetStatusOfVideo(GetStatusOfVideoRequest request, ServerCallContext context)
        {
            PreparedStatement preparedStatement =
                await _statementCache.NoContext.GetOrAddAsync("SELECT newstate, status_date FROM encoding_job_notifications WHERE videoid = ? LIMIT 1");
            RowSet rows = await _session.ExecuteAsync(preparedStatement.Bind(request.VideoId.ToGuid())).ConfigureAwait(false);
            Row row = rows.SingleOrDefault();
            if (row == null)
                return new GetStatusOfVideoResponse();

            return new GetStatusOfVideoResponse
            {
                CurrentState = row.GetValue<string>("newstate"),
                StatusDate = row.GetValue<DateTimeOffset>("status_date").ToTimestamp()
            };
        }
    }
}