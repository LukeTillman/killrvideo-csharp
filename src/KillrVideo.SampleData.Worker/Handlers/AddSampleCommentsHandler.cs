using System;
using System.Threading.Tasks;
using KillrVideo.SampleData.Dtos;
using Nimbus.Handlers;

namespace KillrVideo.SampleData.Worker.Handlers
{
    /// <summary>
    /// Adds sample comments to videos on the site.
    /// </summary>
    public class AddSampleCommentsHandler : IHandleCommand<AddSampleComments>
    {
        public Task Handle(AddSampleComments busCommand)
        {
            throw new NotImplementedException();
        }
    }
}
