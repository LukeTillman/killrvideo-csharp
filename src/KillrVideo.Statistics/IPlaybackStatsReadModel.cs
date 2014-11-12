using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KillrVideo.Statistics.Dtos;

namespace KillrVideo.Statistics
{
    public interface IPlaybackStatsReadModel
    {
        /// <summary>
        /// Gets the number of times the specified video has been played.
        /// </summary>
        Task<PlayStats> GetNumberOfPlays(Guid videoId);

        /// <summary>
        /// Gets the number of times the specified videos have been played.
        /// </summary>
        Task<IEnumerable<PlayStats>> GetNumberOfPlays(ISet<Guid> videoIds);
    }
}
