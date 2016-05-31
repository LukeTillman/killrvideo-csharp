using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using KillrVideo.Comments;
using KillrVideo.Protobuf.Clients;
using KillrVideo.Ratings;
using KillrVideo.Statistics;
using KillrVideo.UserManagement;
using KillrVideo.VideoCatalog;

namespace KillrVideo.SampleData
{
    /// <summary>
    /// Factory that gets service clients needed by message handlers.
    /// </summary>
    [Export(typeof(IServiceClientFactory))]
    public class ServiceClientFactory : IServiceClientFactory
    {
        private readonly IChannelFactory _channelFactory;

        public ServiceClientFactory(IChannelFactory channelFactory)
        {
            if (channelFactory == null) throw new ArgumentNullException(nameof(channelFactory));
            _channelFactory = channelFactory;
        }

        public Task<CommentsService.CommentsServiceClient> GetCommentsClientAsync()
        {
            throw new NotImplementedException();
        }

        public Task<RatingsService.RatingsServiceClient> GetRatingsClientAsync()
        {
            throw new NotImplementedException();
        }

        public Task<UserManagementService.UserManagementServiceClient> GetUsersClientAsync()
        {
            throw new NotImplementedException();
        }

        public Task<StatisticsService.StatisticsServiceClient> GetStatsClientAsync()
        {
            throw new NotImplementedException();
        }

        public Task<VideoCatalogService.VideoCatalogServiceClient> GetVideoClientAsync()
        {
            throw new NotImplementedException();
        }
    }
}
