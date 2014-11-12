using System;
using System.Threading.Tasks;

namespace KillrVideo.Statistics
{
    public interface IPlaybackStatsWriteModel
    {
        /// <summary>
        /// Records that video playback was started for the given video Id.
        /// </summary>
        Task RecordPlaybackStarted(Guid videoId);
    }
}
