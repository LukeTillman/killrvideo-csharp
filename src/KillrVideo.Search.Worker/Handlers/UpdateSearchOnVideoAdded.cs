using System;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.Utils;
using KillrVideo.VideoCatalog.Messages.Events;
using Nimbus.Handlers;

namespace KillrVideo.Search.Worker.Handlers
{
    /// <summary>
    /// Updates the search by tags data when new videos are added to the video catalog.
    /// </summary>
    public class UpdateSearchOnVideoAdded : IHandleMulticastEvent<IVideoAdded>
    {
        private readonly ISession _session;
        private readonly TaskCache<string, PreparedStatement> _statementCache;

        public UpdateSearchOnVideoAdded(ISession session, TaskCache<string, PreparedStatement> statementCache)
        {
            if (session == null) throw new ArgumentNullException("session");
            if (statementCache == null) throw new ArgumentNullException("statementCache");
            _session = session;
            _statementCache = statementCache;
        }

        public async Task Handle(IVideoAdded video)
        {
            PreparedStatement[] prepared = await _statementCache.GetOrAddAllAsync(
                "INSERT INTO videos_by_tag (tag, videoid, added_date, userid, name, preview_image_location, tagged_date) VALUES (?, ?, ?, ?, ?, ?, ?)",
                "INSERT INTO tags_by_letter (first_letter, tag) VALUES (?, ?)");

            // Create a batch for executing the updates
            var batch = new BatchStatement();
            batch.SetTimestamp(video.Timestamp);

            // We need to add multiple statements for each tag
            foreach (string tag in video.Tags)
            {
                // INSERT INTO videos_by_tag
                batch.Add(prepared[0].Bind(tag, video.VideoId, video.Timestamp, video.UserId, video.Name, video.PreviewImageLocation, video.Timestamp));

                // INSERT INTO tags_by_letter
                string firstLetter = tag.Substring(0, 1);
                batch.Add(prepared[1].Bind(firstLetter, tag));
            }

            await _session.ExecuteAsync(batch).ConfigureAwait(false);
        }
    }
}
