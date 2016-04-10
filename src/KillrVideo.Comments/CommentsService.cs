using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using Cassandra;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using KillrVideo.Cassandra;
using KillrVideo.Comments.Events;
using KillrVideo.MessageBus;
using KillrVideo.Protobuf;
using KillrVideo.Protobuf.Services;

namespace KillrVideo.Comments
{
    /// <summary>
    /// Comments service that uses Cassandra to store comments and publishes events on a message bus.
    /// </summary>
    [Export(typeof(IGrpcServerService))]
    public class CommentsServiceImpl : CommentsService.ICommentsService, IGrpcServerService
    {
        private readonly ISession _session;
        private readonly IBus _bus;
        private readonly PreparedStatementCache _statementCache;

        public CommentsServiceImpl(ISession session, PreparedStatementCache statementCache, IBus bus)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (statementCache == null) throw new ArgumentNullException(nameof(statementCache));
            if (bus == null) throw new ArgumentNullException(nameof(bus));
            _session = session;
            _statementCache = statementCache;
            _bus = bus;
        }

        /// <summary>
        /// Convert this instance to a ServerServiceDefinition that can be run on a Grpc server.
        /// </summary>
        public ServerServiceDefinition ToServerServiceDefinition()
        {
            return CommentsService.BindService(this);
        }

        /// <summary>
        /// Records a user comment on a video.
        /// </summary>
        public async Task<CommentOnVideoResponse> CommentOnVideo(CommentOnVideoRequest request, ServerCallContext context)
        {
            // Use a client side timestamp for the writes that we can include when we publish the event
            var timestamp = DateTimeOffset.UtcNow;

            PreparedStatement[] preparedStatements = await _statementCache.GetOrAddAllAsync(
                "INSERT INTO comments_by_video (videoid, commentid, userid, comment) VALUES (?, ?, ?, ?)",
                "INSERT INTO comments_by_user (userid, commentid, videoid, comment) VALUES (?, ?, ?, ?)");

            // Use a batch to insert into all tables
            var batch = new BatchStatement();

            // INSERT INTO comments_by_video
            batch.Add(preparedStatements[0].Bind(request.VideoId.ToGuid(), request.CommentId.ToGuid(), request.UserId.ToGuid(), request.Comment));

            // INSERT INTO comments_by_user
            batch.Add(preparedStatements[1].Bind(request.UserId.ToGuid(), request.CommentId.ToGuid(), request.VideoId.ToGuid(), request.Comment));

            batch.SetTimestamp(timestamp);
            await _session.ExecuteAsync(batch).ConfigureAwait(false);

            // Tell the world about the comment
            await _bus.Publish(new UserCommentedOnVideo
            {
                UserId = request.UserId,
                VideoId = request.VideoId,
                CommentId = request.CommentId,
                CommentTimestamp = timestamp.ToTimestamp()
            }).ConfigureAwait(false);

            return new CommentOnVideoResponse();
        }

        /// <summary>
        /// Gets a page of the latest comments for a user.
        /// </summary>
        public async Task<GetUserCommentsResponse> GetUserComments(GetUserCommentsRequest request, ServerCallContext context)
        {
            PreparedStatement prepared;
            IStatement bound;
            Guid? startingCommentId = request.StartingCommentId.ToNullableGuid();
            Guid userId = request.UserId.ToGuid();
            if (startingCommentId == null)
            {
                prepared = await _statementCache.GetOrAddAsync(
                    "SELECT commentid, videoid, comment, dateOf(commentid) AS comment_timestamp FROM comments_by_user WHERE userid = ?");
                bound = prepared.Bind(userId);
            }
            else
            {
                prepared = await _statementCache.GetOrAddAsync(
                    "SELECT commentid, videoid, comment, dateOf(commentid) AS comment_timestamp FROM comments_by_user WHERE userid = ? AND commentid <= ?");
                bound = prepared.Bind(userId, startingCommentId);
            }

            bound.SetAutoPage(false)
                 .SetPageSize(request.PageSize);

            if (string.IsNullOrEmpty(request.PagingState) == false)
                bound.SetPagingState(Convert.FromBase64String(request.PagingState));
            
            RowSet rows = await _session.ExecuteAsync(bound).ConfigureAwait(false);
            var response = new GetUserCommentsResponse
            {
                UserId = request.UserId,
                PagingState = rows.PagingState != null ? Convert.ToBase64String(rows.PagingState) : ""
            };

            response.Comments.Add(rows.Select(MapRowToUserComment));
            return response;
        }

        /// <summary>
        /// Gets a page of the latest comments for a video.
        /// </summary>
        public async Task<GetVideoCommentsResponse> GetVideoComments(GetVideoCommentsRequest request, ServerCallContext context)
        {
            PreparedStatement prepared;
            IStatement bound;
            Guid? startingCommentId = request.StartingCommentId.ToNullableGuid();
            Guid videoId = request.VideoId.ToGuid();
            if (startingCommentId == null)
            {
                prepared = await _statementCache.GetOrAddAsync(
                    "SELECT commentid, userid, comment, dateOf(commentid) AS comment_timestamp FROM comments_by_video WHERE videoid = ?");
                bound = prepared.Bind(videoId);
            }
            else
            {
                prepared = await _statementCache.GetOrAddAsync(
                    "SELECT commentid, userid, comment, dateOf(commentid) AS comment_timestamp FROM comments_by_video WHERE videoid = ? AND commentid <= ?");
                bound = prepared.Bind(videoId, startingCommentId);
            }

            bound.SetAutoPage(false)
                 .SetPageSize(request.PageSize);

            if (string.IsNullOrEmpty(request.PagingState) == false)
                bound.SetPagingState(Convert.FromBase64String(request.PagingState));

            RowSet rows = await _session.ExecuteAsync(bound).ConfigureAwait(false);
            var response = new GetVideoCommentsResponse
            {
                VideoId = request.VideoId,
                PagingState = rows.PagingState != null ? Convert.ToBase64String(rows.PagingState) : ""
            };
            response.Comments.Add(rows.Select(MapRowToVideoComment));
            return response;
        }

        private static UserComment MapRowToUserComment(Row row)
        {
            if (row == null) return null;

            return new UserComment
            {
                CommentId = row.GetValue<Guid>("commentid").ToTimeUuid(),
                VideoId = row.GetValue<Guid>("videoid").ToUuid(),
                Comment = row.GetValue<string>("comment"),
                CommentTimestamp = row.GetValue<DateTimeOffset>("comment_timestamp").ToTimestamp()
            };
        }

        private static VideoComment MapRowToVideoComment(Row row)
        {
            if (row == null) return null;

            return new VideoComment
            {
                CommentId = row.GetValue<Guid>("commentid").ToTimeUuid(),
                UserId = row.GetValue<Guid>("userid").ToUuid(),
                Comment = row.GetValue<string>("comment"),
                CommentTimestamp = row.GetValue<DateTimeOffset>("comment_timestamp").ToTimestamp()
            };
        }
    }
}