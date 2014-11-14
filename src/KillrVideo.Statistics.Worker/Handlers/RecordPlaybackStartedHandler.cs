using System;
using System.Threading.Tasks;
using KillrVideo.Statistics.Messages.Commands;
using Nimbus.Handlers;

namespace KillrVideo.Statistics.Worker.Handlers
{
    /// <summary>
    /// Records video playback statistics.
    /// </summary>
    public class RecordPlaybackStartedHandler : IHandleCommand<RecordPlaybackStarted>
    {
        private readonly IPlaybackStatsWriteModel _statsWriteModel;

        public RecordPlaybackStartedHandler(IPlaybackStatsWriteModel statsWriteModel)
        {
            if (statsWriteModel == null) throw new ArgumentNullException("statsWriteModel");
            _statsWriteModel = statsWriteModel;
        }

        public Task Handle(RecordPlaybackStarted message)
        {
            return _statsWriteModel.RecordPlaybackStarted(message);
        }
    }
}
