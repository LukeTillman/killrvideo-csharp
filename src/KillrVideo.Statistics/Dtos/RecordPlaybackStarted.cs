using System;

namespace KillrVideo.Statistics.Dtos
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
