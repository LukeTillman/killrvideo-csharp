using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KillrVideo.Statistics.ReadModel.Dtos;

namespace KillrVideo.Statistics.ReadModel
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
