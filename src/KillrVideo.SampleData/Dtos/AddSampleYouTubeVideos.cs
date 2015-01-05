using System;
using Nimbus.MessageContracts;

namespace KillrVideo.SampleData.Dtos
{
    /// <summary>
    /// Command for adding sample YouTube videos to the site.
    /// </summary>
    [Serializable]
    public class AddSampleYouTubeVideos : IBusCommand
    {
        public int NumberOfVideos { get; set; }
    }
}