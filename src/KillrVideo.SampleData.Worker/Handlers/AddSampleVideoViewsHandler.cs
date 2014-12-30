using System;
using System.Threading.Tasks;
using KillrVideo.SampleData.Dtos;
using Nimbus.Handlers;

namespace KillrVideo.SampleData.Worker.Handlers
{
    /// <summary>
    /// Adds sample video views to the site.
    /// </summary>
    public class AddSampleVideoViewsHandler : IHandleCommand<AddSampleVideoViews>
    {
        public Task Handle(AddSampleVideoViews busCommand)
        {
            throw new NotImplementedException();
        }
    }
}