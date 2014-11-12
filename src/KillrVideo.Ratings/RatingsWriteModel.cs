using System;
using System.Linq;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.Ratings.Api.Commands;
using KillrVideo.Utils;

namespace KillrVideo.Ratings
{
    /// <summary>
    /// Handles writes/updates for videos.
    /// </summary>
    public class RatingsWriteModel : IRatingsWriteModel
    {
        private readonly ISession _session;
        
        private readonly AsyncLazy<PreparedStatement[]> _addRatingStatements;

        public RatingsWriteModel(ISession session)
        {
            if (session == null) throw new ArgumentNullException("session");
            _session = session;

            _addRatingStatements = new AsyncLazy<PreparedStatement[]>(PrepareAddRatingStatements);
        }
        
        /// <summary>
        /// Adds a rating for a video.
        /// </summary>
        public async Task RateVideo(RateVideo videoRating)
        {
            PreparedStatement[] preparedStatements = await _addRatingStatements;

            // We can't use a batch here because we can't mix counters with regular DML, but we can run both of them at the same time
            var bound = new[]
            {
                // UPDATE video_ratings... (Driver will throw if we don't cast the rating to a long I guess because counters are data type long)
                preparedStatements[0].Bind((long) videoRating.Rating, videoRating.VideoId),
                // INSERT INTO video_ratings_by_user
                preparedStatements[1].Bind(videoRating.VideoId, videoRating.UserId, videoRating.Rating)
            };

            await Task.WhenAll(bound.Select(b => _session.ExecuteAsync(b)));
        }
        
        private Task<PreparedStatement[]> PrepareAddRatingStatements()
        {
            return Task.WhenAll(new[]
            {
                _session.PrepareAsync("UPDATE video_ratings SET rating_counter = rating_counter + 1, rating_total = rating_total + ? WHERE videoid = ?"),
                _session.PrepareAsync("INSERT INTO video_ratings_by_user (videoid, userid, rating) VALUES (?, ?, ?)")
            });
        }
    }
}