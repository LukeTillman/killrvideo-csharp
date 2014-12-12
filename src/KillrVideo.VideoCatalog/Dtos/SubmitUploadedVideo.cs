using System;
using System.Collections.Generic;
using Nimbus.MessageContracts;

namespace KillrVideo.VideoCatalog.Dtos
{
    /// <summary>
    /// Submits an uploaded video to the catalog.
    /// </summary>
    [Serializable]
    public class SubmitUploadedVideo : IBusCommand
    {
        public Guid VideoId { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public ISet<string> Tags { get; set; }
        public string UploadUrl { get; set; }
    }
}