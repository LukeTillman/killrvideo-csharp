using System;
using System.Collections.Generic;
using Nimbus.MessageContracts;

namespace KillrVideo.VideoCatalog.Messages.Events
{
    /// <summary>
    /// Common interface for events that occur when a video is added to the video catalog.
    /// </summary>
    public interface IVideoAdded : IBusEvent
    {
        Guid VideoId { get; }
        Guid UserId { get; }
        string Name { get; }
        string Description { get; }
        string Location { get; }
        string PreviewImageLocation { get; }
        HashSet<string> Tags { get; }
        DateTimeOffset Timestamp { get; }
    }
}