using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using Cassandra;
using DryIocAttributes;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using KillrVideo.Cassandra;
using KillrVideo.MessageBus;
using KillrVideo.Ratings.Events;

namespace KillrVideo.Ratings
{
    /// <summary>
    /// An implementation of the video ratings service that stores ratings in Cassandra and publishes events on a message bus.
    /// </summary>
    [Export, AsFactory]
    public class RatingsServiceImpl : RatingsService.IRatingsService
    {
        private readonly ISession _session;
        private readonly IBus _bus;
        private readonly PreparedStatementCache _statementCache;

        public RatingsServiceImpl(ISession session, PreparedStatementCache statementCache, IBus bus)
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
        [Export]
        public ServerServiceDefinition ToServerServiceDefinition()
        {
            return RatingsService.BindService(this);
        }

        /// <summary>
        /// Adds a user's rating of a video.
        /// </summary>
        public async Task<RateVideoResponse> RateVideo(RateVideoRequest request, ServerCallContext context)
        {
            PreparedStatement[] preparedStatements = await _statementCache.GetOrAddAllAsync(
                "UPDATE video_ratings SET rating_counter = rating_counter + 1, rating_total = rating_total + ? WHERE videoid = ?",
                "INSERT INTO video_ratings_by_user (videoid, userid, rating) VALUES (?, ?, ?) USING TIMESTAMP ?");

            DateTimeOffset timestamp = DateTimeOffset.UtcNow;

            // We can't use a batch here because we can't mix counters with regular DML, but we can run both of them at the same time
            var bound = new[]
            {
                // UPDATE video_ratings... (Driver will throw if we don't cast the rating to a long I guess because counters are data type long)
                preparedStatements[0].Bind((long) request.Rating, request.VideoId.ToGuid()),
                // INSERT INTO video_ratings_by_user
                preparedStatements[1].Bind(request.VideoId.ToGuid(), request.UserId.ToGuid(), request.Rating, timestamp.ToMicrosecondsSinceEpoch())
            };

            await Task.WhenAll(bound.Select(b => _session.ExecuteAsync(b))).ConfigureAwait(false);

            // Tell the world about the rating
            await _bus.Publish(new UserRatedVideo
            {
                VideoId = request.VideoId,
                UserId = request.UserId,
                Rating = request.Rating,
                RatingTimestamp = timestamp.ToTimestamp()
            }).ConfigureAwait(false);

            return new RateVideoResponse();
        }

        /// <summary>
        /// Gets the current rating stats for the specified video.
        /// </summary>
        public async Task<GetRatingResponse> GetRating(GetRatingRequest request, ServerCallContext context)
        {
            PreparedStatement preparedStatement = await _statementCache.GetOrAddAsync("SELECT * FROM video_ratings WHERE videoid = ?");
            BoundStatement boundStatement = preparedStatement.Bind(request.VideoId.ToGuid());
            RowSet rows = await _session.ExecuteAsync(boundStatement).ConfigureAwait(false);
            Row row = rows.SingleOrDefault();
            
            return new GetRatingResponse
            {
                VideoId = request.VideoId,
                RatingsCount = row?.GetValue<long>("rating_counter") ?? 0,
                RatingsTotal = row?.GetValue<long>("rating_total") ?? 0
            };
        }

        /// <summary>
        /// Gets the rating given by a user for a specific video.  Will return 0 for the rating if the user hasn't rated the video.
        /// </summary>
        public async Task<GetUserRatingResponse> GetUserRating(GetUserRatingRequest request, ServerCallContext context)
        {
            PreparedStatement preparedStatement = await _statementCache.GetOrAddAsync("SELECT rating FROM video_ratings_by_user WHERE videoid = ? AND userid = ?");
            BoundStatement boundStatement = preparedStatement.Bind(request.VideoId.ToGuid(), request.UserId.ToGuid());
            RowSet rows = await _session.ExecuteAsync(boundStatement).ConfigureAwait(false);

            // We may or may not have a rating
            Row row = rows.SingleOrDefault();
            return new GetUserRatingResponse
            {
                VideoId = request.VideoId,
                UserId = request.UserId,
                Rating = row?.GetValue<int>("rating") ?? 0
            };
        }
    }
}