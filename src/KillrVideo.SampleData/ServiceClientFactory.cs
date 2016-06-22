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

        // Cached lazy client instances
        private readonly Lazy<Task<CommentsService.CommentsServiceClient>> _commentsClient;
        private readonly Lazy<Task<RatingsService.RatingsServiceClient>> _ratingsClient;
        private readonly Lazy<Task<UserManagementService.UserManagementServiceClient>> _usersClient;
        private readonly Lazy<Task<StatisticsService.StatisticsServiceClient>> _statsClient;
        private readonly Lazy<Task<VideoCatalogService.VideoCatalogServiceClient>> _videosClient;

        public ServiceClientFactory(IChannelFactory channelFactory)
        {
            if (channelFactory == null) throw new ArgumentNullException(nameof(channelFactory));
            _channelFactory = channelFactory;

            // Create clients on demand and reuse them
            _commentsClient = new Lazy<Task<CommentsService.CommentsServiceClient>>(CreateCommentsClient);
            _ratingsClient = new Lazy<Task<RatingsService.RatingsServiceClient>>(CreateRatingsClient);
            _usersClient = new Lazy<Task<UserManagementService.UserManagementServiceClient>>(CreateUsersClient);
            _statsClient = new Lazy<Task<StatisticsService.StatisticsServiceClient>>(CreateStatsClient);
            _videosClient = new Lazy<Task<VideoCatalogService.VideoCatalogServiceClient>>(CreateVideosClient);
        }

        public Task<CommentsService.CommentsServiceClient> GetCommentsClientAsync()
        {
            return _commentsClient.Value;
        }

        private async Task<CommentsService.CommentsServiceClient> CreateCommentsClient()
        {
            var channel = await _channelFactory.GetChannelAsync(CommentsService.Descriptor).ConfigureAwait(false);
            return CommentsService.NewClient(channel);
        }

        public Task<RatingsService.RatingsServiceClient> GetRatingsClientAsync()
        {
            return _ratingsClient.Value;
        }

        private async Task<RatingsService.RatingsServiceClient> CreateRatingsClient()
        {
            var channel = await _channelFactory.GetChannelAsync(RatingsService.Descriptor).ConfigureAwait(false);
            return RatingsService.NewClient(channel);
        }

        public Task<UserManagementService.UserManagementServiceClient> GetUsersClientAsync()
        {
            return _usersClient.Value;
        }

        private async Task<UserManagementService.UserManagementServiceClient> CreateUsersClient()
        {
            var channel = await _channelFactory.GetChannelAsync(UserManagementService.Descriptor).ConfigureAwait(false);
            return UserManagementService.NewClient(channel);
        }

        public Task<StatisticsService.StatisticsServiceClient> GetStatsClientAsync()
        {
            return _statsClient.Value;
        }

        private async Task<StatisticsService.StatisticsServiceClient> CreateStatsClient()
        {
            var channel = await _channelFactory.GetChannelAsync(StatisticsService.Descriptor).ConfigureAwait(false);
            return StatisticsService.NewClient(channel);
        }

        public Task<VideoCatalogService.VideoCatalogServiceClient> GetVideoClientAsync()
        {
            return _videosClient.Value;
        }

        private async Task<VideoCatalogService.VideoCatalogServiceClient> CreateVideosClient()
        {
            var channel = await _channelFactory.GetChannelAsync(VideoCatalogService.Descriptor).ConfigureAwait(false);
            return VideoCatalogService.NewClient(channel);
        }
    }
}
