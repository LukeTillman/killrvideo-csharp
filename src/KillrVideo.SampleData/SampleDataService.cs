using System;
using System.Threading.Tasks;
using Grpc.Core;
using Nimbus;

namespace KillrVideo.SampleData
{
    /// <summary>
    /// Implementation of sample data service that simply sends commands on the bus to a backend worker.
    /// </summary>
    public class SampleDataServiceImpl : SampleDataService.ISampleDataService
    {
        private readonly IBus _bus;

        public SampleDataServiceImpl(IBus bus)
        {
            if (bus == null) throw new ArgumentNullException(nameof(bus));
            _bus = bus;
        }

        /// <summary>
        /// Adds sample comments to the site.
        /// </summary>
        public Task<AddSampleCommentsResponse> AddSampleComments(AddSampleCommentsRequest request, ServerCallContext context)
        {
            return _bus.Send(request);
        }

        /// <summary>
        /// Adds sample video ratings to the site.
        /// </summary>
        public Task<AddSampleRatingsResponse> AddSampleRatings(AddSampleRatingsRequest request, ServerCallContext context)
        {
            return _bus.Send(request);
        }

        /// <summary>
        /// Adds sample users to the site.
        /// </summary>
        public Task<AddSampleUsersResponse> AddSampleUsers(AddSampleUsersRequest request, ServerCallContext context)
        {
            return _bus.Send(request);
        }

        /// <summary>
        /// Adds sample video views to the site.
        /// </summary>
        public Task<AddSampleVideoViewsResponse> AddSampleVideoViews(AddSampleVideoViewsRequest request, ServerCallContext context)
        {
            return _bus.Send(request);
        }

        /// <summary>
        /// Adds sample YouTube videos to the site.
        /// </summary>
        public Task<AddSampleYouTubeVideosResponse> AddSampleYouTubeVideos(AddSampleYouTubeVideosRequest request, ServerCallContext context)
        {
            return _bus.Send(request);
        }

        /// <summary>
        /// Triggers a refresh of the YouTube sample video sources.
        /// </summary>
        public Task<RefreshYouTubeSourcesResponse> RefreshYouTubeSources(RefreshYouTubeSourcesRequest request, ServerCallContext context)
        {
            return _bus.Send(refresh);
        }
    }
}