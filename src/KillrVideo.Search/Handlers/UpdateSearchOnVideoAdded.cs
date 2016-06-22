using System;
using System.Threading.Tasks;
using Cassandra;
using DryIocAttributes;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using KillrVideo.Cassandra;
using KillrVideo.MessageBus;
using KillrVideo.Protobuf;
using KillrVideo.VideoCatalog.Events;

namespace KillrVideo.Search.Handlers
{
    /// <summary>
    /// Updates the search by tags data when new videos are added to the video catalog.
    /// </summary>
    [ExportMany, Reuse(ReuseType.Transient)]
    public class UpdateSearchOnVideoAdded : IHandleMessage<UploadedVideoAdded>, IHandleMessage<YouTubeVideoAdded>
    {
        private readonly ISession _session;
        private readonly PreparedStatementCache _statementCache;

        public UpdateSearchOnVideoAdded(ISession session, PreparedStatementCache statementCache)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (statementCache == null) throw new ArgumentNullException(nameof(statementCache));
            _session = session;
            _statementCache = statementCache;
        }

        private async Task HandleImpl(Uuid videoId, Timestamp addedDate, Uuid userId, string name, string previewImageLocation,
                                      RepeatedField<string> tags, Timestamp timestamp)
        {
            PreparedStatement[] prepared = await _statementCache.GetOrAddAllAsync(
                "INSERT INTO videos_by_tag (tag, videoid, added_date, userid, name, preview_image_location, tagged_date) VALUES (?, ?, ?, ?, ?, ?, ?)",
                "INSERT INTO tags_by_letter (first_letter, tag) VALUES (?, ?)");

            DateTimeOffset ts = timestamp.ToDateTimeOffset();

            // Create a batch for executing the updates
            var batch = new BatchStatement();
            
            // We need to add multiple statements for each tag
            foreach (string tag in tags)
            {
                // INSERT INTO videos_by_tag
                batch.Add(prepared[0].Bind(tag, videoId.ToGuid(), addedDate.ToDateTimeOffset(), userId.ToGuid(), name, previewImageLocation, ts));

                // INSERT INTO tags_by_letter
                string firstLetter = tag.Substring(0, 1);
                batch.Add(prepared[1].Bind(firstLetter, tag));
            }
            batch.SetTimestamp(ts);

            await _session.ExecuteAsync(batch).ConfigureAwait(false);
        }

        public Task Handle(UploadedVideoAdded busEvent)
        {
            return HandleImpl(busEvent.VideoId, busEvent.AddedDate, busEvent.UserId, busEvent.Name, busEvent.PreviewImageLocation,
                              busEvent.Tags, busEvent.Timestamp);
        }

        public Task Handle(YouTubeVideoAdded busEvent)
        {
            return HandleImpl(busEvent.VideoId, busEvent.AddedDate, busEvent.UserId, busEvent.Name, busEvent.PreviewImageLocation,
                              busEvent.Tags, busEvent.Timestamp);
        }
    }
}
