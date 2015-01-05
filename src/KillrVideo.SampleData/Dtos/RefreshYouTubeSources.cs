using System;
using Nimbus.MessageContracts;

namespace KillrVideo.SampleData.Dtos
{
    /// <summary>
    /// Command that will trigger a refresh of sample YouTube videos from available sources.
    /// </summary>
    [Serializable]
    public class RefreshYouTubeSources : IBusCommand
    {
    }
}
