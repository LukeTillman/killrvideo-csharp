using System;

namespace KillrVideo.Data.PlaybackStats.Dtos
{
    /// <summary>
    /// The playback stats for a video.
    /// </summary>
    [Serializable]
    public class PlayStats
    {
        public Guid VideoId { get; set; }
        public long Views { get; set; }
    }
}
