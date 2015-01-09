using System;
using System.Linq;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.Comments.Dtos;
using KillrVideo.Comments.Messages.Events;
using KillrVideo.Utils;
using Nimbus;

namespace KillrVideo.Comments
{
    /// <summary>
    /// Comments service that uses Cassandra to store comments and publishes events on a message bus.
    /// </summary>
    public class CommentsService : ICommentsService
    {
        private readonly ISession _session;
        private readonly TaskCache<string, PreparedStatement> _statementCache;
        private readonly IBus _bus;

        public CommentsService(ISession session, TaskCache<string, PreparedStatement> statementCache, IBus bus)
        {
            if (session == null) throw new ArgumentNullException("session");
            if (statementCache == null) throw new ArgumentNullException("statementCache");
            if (bus == null) throw new ArgumentNullException("bus");
            _session = session;
            _statementCache = statementCache;
            _bus = bus;
        }

        /// <summary>
        /// Records a user comment on a video.
        /// </summary>
        public async Task CommentOnVideo(CommentOnVideo comment)
        {
            // Use a client side timestamp for the writes that we can include when we publish the event
            var timestamp = DateTimeOffset.UtcNow;

            PreparedStatement[] preparedStatements = await _statementCache.NoContext.GetOrAddAllAsync(
                "INSERT INTO comments_by_video (videoid, commentid, userid, comment) VALUES (?, ?, ?, ?) USING TIMESTAMP ?",
                "INSERT INTO comments_by_user (userid, commentid, videoid, comment) VALUES (?, ?, ?, ?) USING TIMESTAMP ?");

            // Use a batch to insert into all tables
            var batch = new BatchStatement();

            // INSERT INTO comments_by_video
            batch.Add(preparedStatements[0].Bind(comment.VideoId, comment.CommentId, comment.UserId, comment.Comment,
                                                 timestamp.ToMicrosecondsSinceEpoch()));

            // INSERT INTO comments_by_user
            batch.Add(preparedStatements[1].Bind(comment.UserId, comment.CommentId, comment.VideoId, comment.Comment,
                                                 timestamp.ToMicrosecondsSinceEpoch()));

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

        /// <summary>
        /// Gets a page of the latest comments for a user.
        /// </summary>
        public async Task<UserComments> GetUserComments(GetUserComments getComments)
        {
            PreparedStatement prepared;
            BoundStatement bound;

            if (getComments.FirstCommentIdOnPage.HasValue)
            {
                prepared = await _statementCache.NoContext.GetOrAddAsync(
                    "SELECT commentid, videoid, comment, dateOf(commentid) AS comment_timestamp FROM comments_by_user WHERE userid = ? AND commentid <= ? LIMIT ?");
                bound = prepared.Bind(getComments.UserId, getComments.FirstCommentIdOnPage.Value, getComments.PageSize);
            }
            else
            {
                prepared = await _statementCache.NoContext.GetOrAddAsync(
                    "SELECT commentid, videoid, comment, dateOf(commentid) AS comment_timestamp FROM comments_by_user WHERE userid = ? LIMIT ?");
                bound = prepared.Bind(getComments.UserId, getComments.PageSize);
            }

            RowSet rows = await _session.ExecuteAsync(bound);
            return new UserComments
            {
                UserId = getComments.UserId,
                Comments = rows.Select(MapRowToUserComment).ToList()
            };
        }

        /// <summary>
        /// Gets a page of the latest comments for a video.
        /// </summary>
        public async Task<VideoComments> GetVideoComments(GetVideoComments getComments)
        {
            PreparedStatement prepared;
            BoundStatement bound;

            if (getComments.FirstCommentIdOnPage.HasValue)
            {
                prepared = await _statementCache.NoContext.GetOrAddAsync(
                    "SELECT commentid, userid, comment, dateOf(commentid) AS comment_timestamp FROM comments_by_video WHERE videoid = ? AND commentid <= ? LIMIT ?");
                bound = prepared.Bind(getComments.VideoId, getComments.FirstCommentIdOnPage.Value, getComments.PageSize);
            }
            else
            {
                prepared = await _statementCache.NoContext.GetOrAddAsync(
                    "SELECT commentid, userid, comment, dateOf(commentid) AS comment_timestamp FROM comments_by_video WHERE videoid = ? LIMIT ?");
                bound = prepared.Bind(getComments.VideoId, getComments.PageSize);
            }

            RowSet rows = await _session.ExecuteAsync(bound);
            return new VideoComments
            {
                VideoId = getComments.VideoId,
                Comments = rows.Select(MapRowToVideoComment).ToList()
            };
        }

        private static UserComment MapRowToUserComment(Row row)
        {
            if (row == null) return null;

            return new UserComment
            {
                CommentId = row.GetValue<Guid>("commentid"),
                VideoId = row.GetValue<Guid>("videoid"),
                Comment = row.GetValue<string>("comment"),
                CommentTimestamp = row.GetValue<DateTimeOffset>("comment_timestamp")
            };
        }

        private static VideoComment MapRowToVideoComment(Row row)
        {
            if (row == null) return null;

            return new VideoComment
            {
                CommentId = row.GetValue<Guid>("commentid"),
                UserId = row.GetValue<Guid>("userid"),
                Comment = row.GetValue<string>("comment"),
                CommentTimestamp = row.GetValue<DateTimeOffset>("comment_timestamp")
            };
        }
    }
}