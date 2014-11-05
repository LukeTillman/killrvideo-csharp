using System;

namespace KillrVideo.Models.PlaybackStats
{
    /// <summary>
    /// ViewModel for recording video playback starting.
    /// </summary>
    [Serializable]
    public class StartedViewModel
    {
        public Guid VideoId { get; set; }
    }
}