using System;
using System.Linq;
using System.Threading.Tasks;
using KillrVideo.MessageBus;
using KillrVideo.SampleData.Components.YouTube;

namespace KillrVideo.SampleData.Handlers
{
    /// <summary>
    /// Handler that refreshes sample YouTube video sources.
    /// </summary>
    public class RefreshYouTubeSourcesHandler : IHandleMessage<RefreshYouTubeSourcesRequest>
    {
        private readonly SampleYouTubeVideoManager _youTubeManager;

        public RefreshYouTubeSourcesHandler(SampleYouTubeVideoManager youTubeManager)
        {
            if (youTubeManager == null) throw new ArgumentNullException(nameof(youTubeManager));
            _youTubeManager = youTubeManager;
        }

        public Task Handle(RefreshYouTubeSourcesRequest busCommand)
        {
            var refreshTasks = YouTubeVideoSource.All.Select(s => s.RefreshVideos(_youTubeManager));
            return Task.WhenAll(refreshTasks);
        }
    }
}
