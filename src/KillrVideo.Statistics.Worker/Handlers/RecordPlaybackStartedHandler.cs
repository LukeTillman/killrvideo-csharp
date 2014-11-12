using System;
using KillrVideo.Statistics.Messages.Commands;
using Rebus;

namespace KillrVideo.Statistics.Worker.Handlers
{
    /// <summary>
    /// Records video playback statistics.
    /// </summary>
    public class RecordPlaybackStartedHandler : IHandleMessages<RecordPlaybackStarted>
    {
        private readonly IPlaybackStatsWriteModel _statsWriteModel;

        public RecordPlaybackStartedHandler(IPlaybackStatsWriteModel statsWriteModel)
        {
            if (statsWriteModel == null) throw new ArgumentNullException("statsWriteModel");
            _statsWriteModel = statsWriteModel;
        }

        public void Handle(RecordPlaybackStarted message)
        {
            _statsWriteModel.RecordPlaybackStarted(message);
        }
    }
}
