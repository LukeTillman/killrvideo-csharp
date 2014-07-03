using System;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.Data.Comments.Dtos;

namespace KillrVideo.Data.Comments
{
    /// <summary>
    /// Handles writes/updates for comments.
    /// </summary>
    public class CommentWriteModel : ICommentWriteModel
    {
        private readonly ISession _session;

        private readonly AsyncLazy<PreparedStatement[]> _addCommentStatements;

        public CommentWriteModel(ISession session)
        {
            if (session == null) throw new ArgumentNullException("session");
            _session = session;

            // Some reusable prepared statements
            _addCommentStatements = new AsyncLazy<PreparedStatement[]>(PrepareAddCommentStatements);
        }

        /// <summary>
        /// Adds a comment on a video.  Returns an unique Id for the comment.
        /// </summary>
        public async Task<Guid> CommentOnVideo(CommentOnVideo comment)
        {
            PreparedStatement[] preparedStatements = await _addCommentStatements;

            // Generate a TimeUUID for the comment Id
            Guid commentId = GuidGenerator.GenerateTimeBasedGuid(comment.CommentTimestamp);

            // Use a batch to insert into all tables
            var batch = new BatchStatement();

            // INSERT INTO comments_by_video
            batch.Add(preparedStatements[0].Bind(comment.VideoId, commentId, comment.UserId, comment.Comment));

            // INSERT INTO comments_by_user
            batch.Add(preparedStatements[1].Bind(comment.UserId, commentId, comment.VideoId, comment.Comment));

            await _session.ExecuteAsync(batch);
            return commentId;
        }

        private Task<PreparedStatement[]> PrepareAddCommentStatements()
        {
            return Task.WhenAll(new[]
            {
                _session.PrepareAsync("INSERT INTO comments_by_video (videoid, commentid, userid, comment) VALUES (?, ?, ?, ?)"),
                _session.PrepareAsync("INSERT INTO comments_by_user (userid, commentid, videoid, comment) VALUES (?, ?, ?, ?)")
            });
        }
    }
}