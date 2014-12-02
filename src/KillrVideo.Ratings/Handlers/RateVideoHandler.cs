using System;
using System.Linq;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.Ratings.Messages.Commands;
using KillrVideo.Ratings.Messages.Events;
using KillrVideo.Utils;
using Nimbus;
using Nimbus.Handlers;

namespace KillrVideo.Ratings.Handlers
{
    /// <summary>
    /// Records a user rating a video.
    /// </summary>
    public class RateVideoHandler : IHandleCommand<RateVideo>
    {
        private readonly ISession _session;
        private readonly TaskCache<string, PreparedStatement> _statementCache;
        private readonly IBus _bus;

        public RateVideoHandler(ISession session, TaskCache<string, PreparedStatement> statementCache, IBus bus)
        {
            if (session == null) throw new ArgumentNullException("session");
            if (statementCache == null) throw new ArgumentNullException("statementCache");
            if (bus == null) throw new ArgumentNullException("bus");
            _session = session;
            _statementCache = statementCache;
            _bus = bus;
        }

        public async Task Handle(RateVideo videoRating)
        {
            PreparedStatement[] preparedStatements = await _statementCache.NoContext.GetOrAddAllAsync(
                "UPDATE video_ratings SET rating_counter = rating_counter + 1, rating_total = rating_total + ? WHERE videoid = ?",
                "INSERT INTO video_ratings_by_user (videoid, userid, rating) VALUES (?, ?, ?)");

            DateTimeOffset timestamp = DateTimeOffset.UtcNow;

            // We can't use a batch here because we can't mix counters with regular DML, but we can run both of them at the same time
            var bound = new[]
            {
                // UPDATE video_ratings... (Driver will throw if we don't cast the rating to a long I guess because counters are data type long)
                preparedStatements[0].Bind((long) videoRating.Rating, videoRating.VideoId).SetTimestamp(timestamp),
                // INSERT INTO video_ratings_by_user
                preparedStatements[1].Bind(videoRating.VideoId, videoRating.UserId, videoRating.Rating).SetTimestamp(timestamp)
            };

            await Task.WhenAll(bound.Select(b => _session.ExecuteAsync(b)));

            // Tell the world about the rating
            await _bus.Publish(new UserRatedVideo
            {
                VideoId = videoRating.VideoId,
                UserId = videoRating.UserId,
                Rating = videoRating.Rating,
                Timestamp = timestamp
            });
        }
    }
}
