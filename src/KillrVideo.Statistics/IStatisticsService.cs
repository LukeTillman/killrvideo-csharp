using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KillrVideo.Statistics.Dtos;

namespace KillrVideo.Statistics
{
    /// <summary>
    /// The public API for the video statistics service.
    /// </summary>
    public interface IStatisticsService
    {
        /// <summary>
        /// Records that playback has been started for a video.
        /// </summary>
        Task RecordPlaybackStarted(RecordPlaybackStarted playback);

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
