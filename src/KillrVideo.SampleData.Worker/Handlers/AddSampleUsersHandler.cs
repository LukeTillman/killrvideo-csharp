using System;
using System.Threading.Tasks;
using KillrVideo.SampleData.Dtos;
using Nimbus.Handlers;

namespace KillrVideo.SampleData.Worker.Handlers
{
    /// <summary>
    /// Adds sample users to the site.
    /// </summary>
    public class AddSampleUsersHandler : IHandleCommand<AddSampleUsers>
    {
        public Task Handle(AddSampleUsers busCommand)
        {
            throw new NotImplementedException();
        }
    }
}