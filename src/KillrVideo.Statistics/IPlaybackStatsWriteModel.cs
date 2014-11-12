using System.Threading.Tasks;
using KillrVideo.Statistics.Messages.Commands;

namespace KillrVideo.Statistics
{
    public interface IPlaybackStatsWriteModel
    {
        /// <summary>
        /// Records that video playback was started for the given video Id.
        /// </summary>
        Task RecordPlaybackStarted(RecordPlaybackStarted playback);
    }
}
