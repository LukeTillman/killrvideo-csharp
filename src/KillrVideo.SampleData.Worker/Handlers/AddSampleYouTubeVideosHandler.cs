using System;
using System.Threading.Tasks;
using KillrVideo.SampleData.Dtos;
using Nimbus.Handlers;

namespace KillrVideo.SampleData.Worker.Handlers
{
    /// <summary>
    /// Adds sample YouTube videos to the site.
    /// </summary>
    public class AddSampleYouTubeVideosHandler : IHandleCommand<AddSampleYouTubeVideos>
    {
        public Task Handle(AddSampleYouTubeVideos busCommand)
        {
            throw new NotImplementedException();
        }
    }
}