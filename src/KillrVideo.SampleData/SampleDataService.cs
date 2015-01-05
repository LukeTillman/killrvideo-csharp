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

        /// <summary>
        /// Adds sample comments to the site.
        /// </summary>
        public Task AddSampleComments(AddSampleComments comments)
        {
            return _bus.Send(comments);
        }

        /// <summary>
        /// Adds sample video ratings to the site.
        /// </summary>
        public Task AddSampleRatings(AddSampleRatings ratings)
        {
            return _bus.Send(ratings);
        }

        /// <summary>
        /// Adds sample users to the site.
        /// </summary>
        public Task AddSampleUsers(AddSampleUsers users)
        {
            return _bus.Send(users);
        }

        /// <summary>
        /// Adds sample video views to the site.
        /// </summary>
        public Task AddSampleVideoViews(AddSampleVideoViews views)
        {
            return _bus.Send(views);
        }

        /// <summary>
        /// Adds sample YouTube videos to the site.
        /// </summary>
        public Task AddSampleYouTubeVideos(AddSampleYouTubeVideos videos)
        {
            return _bus.Send(videos);
        }

        /// <summary>
        /// Triggers a refresh of the YouTube sample video sources.
        /// </summary>
        public Task RefreshYouTubeSources(RefreshYouTubeSources refresh)
        {
            return _bus.Send(refresh);
        }
    }
}