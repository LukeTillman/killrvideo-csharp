using System;
using Nimbus.MessageContracts;

namespace KillrVideo.Statistics.Dtos
{
    /// <summary>
    /// Command for recording a playback of a video.
    /// </summary>
    [Serializable]
    public class RecordPlaybackStarted : IBusCommand
    {
        public Guid VideoId { get; set; }
    }
}
