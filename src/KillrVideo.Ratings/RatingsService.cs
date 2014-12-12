using System;
using System.Linq;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.Ratings.Dtos;
using KillrVideo.Ratings.Messages.Events;
using KillrVideo.Utils;
using Nimbus;

namespace KillrVideo.Ratings
{
    /// <summary>
    /// An implementation of the video ratings service that stores ratings in Cassandra and publishes events on a message bus.
    /// </summary>
    public class RatingsService : IRatingsService
    {
        private readonly ISession _session;
        private readonly TaskCache<string, PreparedStatement> _statementCache;
        private readonly IBus _bus;

        public RatingsService(ISession session, TaskCache<string, PreparedStatement> statementCache, IBus bus)
        {
            if (session == null) throw new ArgumentNullException("session");
            if (statementCache == null) throw new ArgumentNullException("statementCache");
            if (bus == null) throw new ArgumentNullException("bus");
            _session = session;
            _statementCache = statementCache;
            _bus = bus;
        }

        /// <summary>
        /// Adds a user's rating of a video.
        /// </summary>
        public async Task RateVideo(RateVideo videoRating)
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

        /// <summary>
        /// Gets the current rating stats for the specified video.
        /// </summary>
        public async Task<VideoRating> GetRating(Guid videoId)
        {
            PreparedStatement preparedStatement = await _statementCache.NoContext.GetOrAddAsync("SELECT * FROM video_ratings WHERE videoid = ?");
            BoundStatement boundStatement = preparedStatement.Bind(videoId);
            RowSet rows = await _session.ExecuteAsync(boundStatement);

            // Use SingleOrDefault here because it's possible a video doesn't have any ratings yet and thus has no record
            return MapRowToVideoRating(rows.SingleOrDefault(), videoId);
        }

        /// <summary>
        /// Gets the rating given by a user for a specific video.  Will return 0 for the rating if the user hasn't rated the video.
        /// </summary>
        public async Task<UserVideoRating> GetRatingFromUser(Guid videoId, Guid userId)
        {
            PreparedStatement preparedStatement = await _statementCache.NoContext.GetOrAddAsync("SELECT rating FROM video_ratings_by_user WHERE videoid = ? AND userid = ?");
            BoundStatement boundStatement = preparedStatement.Bind(videoId, userId);
            RowSet rows = await _session.ExecuteAsync(boundStatement);

            // We may or may not have a rating
            Row row = rows.SingleOrDefault();
            return new UserVideoRating
            {
                VideoId = videoId,
                UserId = userId,
                Rating = row == null ? 0 : row.GetValue<int>("rating")
            };
        }

        /// <summary>
        /// Maps a row to a VideoRating object.
        /// </summary>
        private static VideoRating MapRowToVideoRating(Row row, Guid videoId)
        {
            // If we get null, just return an object with 0s as the rating tallys
            if (row == null)
                return new VideoRating { VideoId = videoId, RatingsCount = 0, RatingsTotal = 0 };

            return new VideoRating
            {
                VideoId = videoId,
                RatingsCount = row.GetValue<long>("rating_counter"),
                RatingsTotal = row.GetValue<long>("rating_total")
            };
        }
    }
}