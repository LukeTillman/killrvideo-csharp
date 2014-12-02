using System;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.Comments.Messages.Commands;
using KillrVideo.Comments.Messages.Events;
using KillrVideo.Utils;
using Nimbus;
using Nimbus.Handlers;

namespace KillrVideo.Comments.Handlers
{
    /// <summary>
    /// Handles recording user comments on videos.
    /// </summary>
    public class CommentOnVideoHandler : IHandleCommand<CommentOnVideo>
    {
        private readonly ISession _session;
        private readonly TaskCache<string, PreparedStatement> _statementCache;
        private readonly IBus _bus;
        
        public CommentOnVideoHandler(ISession session, TaskCache<string, PreparedStatement> statementCache, IBus bus)
        {
            if (session == null) throw new ArgumentNullException("session");
            if (statementCache == null) throw new ArgumentNullException("statementCache");
            if (bus == null) throw new ArgumentNullException("bus");
            _session = session;
            _statementCache = statementCache;
            _bus = bus;
        }

        public async Task Handle(CommentOnVideo comment)
        {
            PreparedStatement[] preparedStatements = await _statementCache.NoContext.GetOrAddAllAsync(
                "INSERT INTO comments_by_video (videoid, commentid, userid, comment) VALUES (?, ?, ?, ?)",
                "INSERT INTO comments_by_user (userid, commentid, videoid, comment) VALUES (?, ?, ?, ?)");

            // Use a batch to insert into all tables
            var batch = new BatchStatement();

            // INSERT INTO comments_by_video
            batch.Add(preparedStatements[0].Bind(comment.VideoId, comment.CommentId, comment.UserId, comment.Comment));

            // INSERT INTO comments_by_user
            batch.Add(preparedStatements[1].Bind(comment.UserId, comment.CommentId, comment.VideoId, comment.Comment));

            // Use a client side timestamp for the writes that we can include when we publish the event
            var timestamp = DateTimeOffset.UtcNow;
            batch.SetTimestamp(timestamp);

            await _session.ExecuteAsync(batch);

            // Tell the world about the comment
            await _bus.Publish(new UserCommentedOnVideo
            {
                UserId = comment.UserId,
                VideoId = comment.VideoId,
                CommentId = comment.CommentId,
                Timestamp = timestamp
            });
        }
    }
}
