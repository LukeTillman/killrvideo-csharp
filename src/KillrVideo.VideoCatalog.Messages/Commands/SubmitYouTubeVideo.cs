using System;
using System.Collections.Generic;
using Nimbus.MessageContracts;

namespace KillrVideo.VideoCatalog.Messages.Commands
{
    /// <summary>
    /// Submits a YouTube video to the catalog.
    /// </summary>
    [Serializable]
    public class SubmitYouTubeVideo : IBusCommand
    {
        public Guid VideoId { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public ISet<string> Tags { get; set; }
        public string YouTubeVideoId { get; set; }
    }
}