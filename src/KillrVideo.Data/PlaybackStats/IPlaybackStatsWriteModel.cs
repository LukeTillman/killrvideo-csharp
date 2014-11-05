using System;
using System.Threading.Tasks;

namespace KillrVideo.Data.PlaybackStats
{
    public interface IPlaybackStatsWriteModel
    {
        /// <summary>
        /// Records that video playback was started for the given video Id.
        /// </summary>
        Task RecordPlaybackStarted(Guid videoId);
    }
}
