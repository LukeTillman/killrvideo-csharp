using System;

namespace KillrVideo.Statistics.Messages.Commands
{
    /// <summary>
    /// Command for recording a playback of a video.
    /// </summary>
    [Serializable]
    public class RecordPlaybackStarted
    {
        public Guid VideoId { get; set; }
    }
}
