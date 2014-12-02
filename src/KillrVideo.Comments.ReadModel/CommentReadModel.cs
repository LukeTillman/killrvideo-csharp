using System;
using System.Linq;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.Comments.ReadModel.Dtos;
using KillrVideo.Utils;

namespace KillrVideo.Comments.ReadModel
{
    /// <summary>
    /// Handles reading data from Cassandra for comments.
    /// </summary>
    public class CommentReadModel : ICommentReadModel
    {
        private readonly ISession _session;
        private readonly TaskCache<string, PreparedStatement> _statementCache;

        public CommentReadModel(ISession session, TaskCache<string, PreparedStatement> statementCache)
        {
            if (session == null) throw new ArgumentNullException("session");
            if (statementCache == null) throw new ArgumentNullException("statementCache");
            _session = session;
            _statementCache = statementCache;
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