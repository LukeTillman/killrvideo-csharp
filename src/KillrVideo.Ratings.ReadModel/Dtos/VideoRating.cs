using System;

namespace KillrVideo.Ratings.ReadModel.Dtos
{
    /// <summary>
    /// Represents the current rating stats for a given video.
    /// </summary>
    [Serializable]
    public class VideoRating
    {
        /// <summary>
        /// The video Id these ratings are for.
        /// </summary>
        public Guid VideoId { get; set; }

        /// <summary>
        /// The number of ratings that have been recorded.
        /// </summary>
        public long RatingsCount { get; set; }

        /// <summary>
        /// The total sum of all the ratings that have been recorded.
        /// </summary>
        public long RatingsTotal { get; set; }
    }
}
