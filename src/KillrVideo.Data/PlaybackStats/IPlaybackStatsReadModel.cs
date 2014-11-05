using System;
using System.Threading.Tasks;

namespace KillrVideo.Data.PlaybackStats
{
    public interface IPlaybackStatsReadModel
    {
        /// <summary>
        /// Gets the number of times the specified video has been played.
        /// </summary>
        Task<long> GetNumberOfPlays(Guid videoId);
    }
}
