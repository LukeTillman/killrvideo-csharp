using System;
using System.Collections.Generic;
using Nimbus.MessageContracts;

namespace KillrVideo.VideoCatalog.Messages.Commands
{
    /// <summary>
    /// DTO for adding a new video.
    /// </summary>
    [Serializable]
    public class AddVideo : IBusCommand
    {
        public Guid VideoId { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public VideoLocationType LocationType { get; set; }
        public ISet<string> Tags { get; set; }
        public string PreviewImageLocation { get; set; }
        
        public AddVideo()
        {
            Tags = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
        }
    }
}
