using System;
using System.Linq;
using System.Threading.Tasks;
using KillrVideo.SampleData.Dtos;
using KillrVideo.SampleData.Worker.Components.YouTube;
using Nimbus.Handlers;

namespace KillrVideo.SampleData.Worker.Handlers
{
    /// <summary>
    /// Handler that refreshes sample YouTube video sources.
    /// </summary>
    public class RefreshYouTubeSourcesHandler : IHandleCommand<RefreshYouTubeSources>
    {
        private readonly SampleYouTubeVideoManager _youTubeManager;

        public RefreshYouTubeSourcesHandler(SampleYouTubeVideoManager youTubeManager)
        {
            if (youTubeManager == null) throw new ArgumentNullException("youTubeManager");
            _youTubeManager = youTubeManager;
        }

        public Task Handle(RefreshYouTubeSources busCommand)
        {
            var refreshTasks = YouTubeVideoSource.All.Select(s => s.RefreshVideos(_youTubeManager));
            return Task.WhenAll(refreshTasks);
        }
    }
}
