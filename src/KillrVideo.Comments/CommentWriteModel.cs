using System;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.Comments.Messages.Commands;
using KillrVideo.Comments.Messages.Events;
using KillrVideo.Utils;
using Nimbus;

namespace KillrVideo.Comments
{
    /// <summary>
    /// Handles writes/updates for comments.
    /// </summary>
    public class CommentWriteModel : ICommentWriteModel
    {
        private readonly ISession _session;
        private readonly IBus _bus;

        private readonly AsyncLazy<PreparedStatement[]> _addCommentStatements;

        public CommentWriteModel(ISession session, IBus bus)
        {
            if (session == null) throw new ArgumentNullException("session");
            if (bus == null) throw new ArgumentNullException("bus");
            _session = session;
            _bus = bus;

            // Some reusable prepared statements
            _addCommentStatements = new AsyncLazy<PreparedStatement[]>(() => Task.WhenAll(new[]
            {
                _session.PrepareAsync("INSERT INTO comments_by_video (videoid, commentid, userid, comment) VALUES (?, ?, ?, ?)"),
                _session.PrepareAsync("INSERT INTO comments_by_user (userid, commentid, videoid, comment) VALUES (?, ?, ?, ?)")
            }));
        }

        /// <summary>
        /// Adds a comment on a video.  Returns an unique Id for the comment.
        /// </summary>
        public async Task CommentOnVideo(CommentOnVideo comment)
        {
            PreparedStatement[] preparedStatements = await _addCommentStatements;

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