using System;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.Utils;
using KillrVideo.VideoCatalog.Messages.Commands;
using KillrVideo.VideoCatalog.Messages.Events;
using Nimbus;
using Nimbus.Handlers;

namespace KillrVideo.VideoCatalog.Handlers
{
    /// <summary>
    /// Adds an uploaded video to the catalog.
    /// </summary>
    public class SubmitUploadedVideoHandler : IHandleCommand<SubmitUploadedVideo>
    {
        private readonly ISession _session;
        private readonly TaskCache<string, PreparedStatement> _statementCache;
        private readonly IBus _bus;

        public SubmitUploadedVideoHandler(ISession session, TaskCache<string, PreparedStatement> statementCache, IBus bus)
        {
            if (session == null) throw new ArgumentNullException("session");
            if (statementCache == null) throw new ArgumentNullException("statementCache");
            _session = session;
            _statementCache = statementCache;
            _bus = bus;
        }

        public async Task Handle(SubmitUploadedVideo uploadedVideo)
        {
            // Store the information we have now in Cassandra
            PreparedStatement prepared = await _statementCache.NoContext.GetOrAddAsync(
                "INSERT INTO videos (videoid, userid, name, description, tags, location_type) VALUES (?, ?, ?, ?, ?, " +
                VideoCatalogConstants.UploadedVideoType + ")");

            BoundStatement bound = prepared.Bind(uploadedVideo.VideoId, uploadedVideo.UserId, uploadedVideo.Name, uploadedVideo.Description,
                                                 uploadedVideo.Tags);
            bound.SetTimestamp(DateTimeOffset.UtcNow);
            await _session.ExecuteAsync(bound);

            // Tell the world we've accepted an uploaded video (it hasn't officially been added until we get a location for the
            // video playback and thumbnail)
            await _bus.Publish(new UploadedVideoAccepted
            {
                VideoId = uploadedVideo.VideoId,
                UploadUrl = uploadedVideo.UploadUrl,
                Timestamp = bound.Timestamp.Value
            });
        }
    }
}