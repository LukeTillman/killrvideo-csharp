using System;
using System.Threading.Tasks;
using KillrVideo.SampleData.Dtos;
using Nimbus.Handlers;

namespace KillrVideo.SampleData.Worker.Handlers
{
    /// <summary>
    /// Adds sample video ratings to the site.
    /// </summary>
    public class AddSampleRatingsHandler : IHandleCommand<AddSampleRatings>
    {
        public Task Handle(AddSampleRatings busCommand)
        {
            throw new NotImplementedException();
        }
    }
}