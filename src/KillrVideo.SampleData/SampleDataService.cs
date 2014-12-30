using System;
using System.Threading.Tasks;
using KillrVideo.SampleData.Dtos;
using Nimbus;

namespace KillrVideo.SampleData
{
    /// <summary>
    /// Implementation of sample data service that simply sends commands on the bus to a backend worker.
    /// </summary>
    public class SampleDataService : ISampleDataService
    {
        private readonly IBus _bus;

        public SampleDataService(IBus bus)
        {
            if (bus == null) throw new ArgumentNullException("bus");
            _bus = bus;
        }

        public Task AddSampleComments(AddSampleComments comments)
        {
            return _bus.Send(comments);
        }

        public Task AddSampleRatings(AddSampleRatings ratings)
        {
            return _bus.Send(ratings);
        }

        public Task AddSampleUsers(AddSampleUsers users)
        {
            return _bus.Send(users);
        }

        public Task AddSampleVideoViews(AddSampleVideoViews views)
        {
            return _bus.Send(views);
        }

        public Task AddSampleYouTubeVideos(AddSampleYouTubeVideos videos)
        {
            return _bus.Send(videos);
        }
    }
}