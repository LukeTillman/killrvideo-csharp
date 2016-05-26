using System.ComponentModel.Composition;
using DryIocAttributes;
using Grpc.Core;
using KillrVideo.Comments;
using KillrVideo.Protobuf.Clients;
using KillrVideo.Ratings;
using KillrVideo.Statistics;
using KillrVideo.UserManagement;
using KillrVideo.VideoCatalog;

namespace KillrVideo.SampleData
{
    /// <summary>
    /// Static factory with methods for getting all the service clients needed by the SampleData message handlers.
    /// </summary>
    [Export, AsFactory]
    public static class ServiceClientFactory
    {
        [Export]
        public static CommentsService.CommentsServiceClient CreateCommentsClient(IChannelFactory channelFactory)
        {
            Channel channel = channelFactory.GetChannel(CommentsService.Descriptor);
            return CommentsService.NewClient(channel);
        }

        [Export]
        public static RatingsService.RatingsServiceClient CreateRatingsClient(IChannelFactory channelFactory)
        {
            Channel channel = channelFactory.GetChannel(RatingsService.Descriptor);
            return RatingsService.NewClient(channel);
        }

        [Export]
        public static UserManagementService.UserManagementServiceClient CreateUsersClient(IChannelFactory channelFactory)
        {
            Channel channel = channelFactory.GetChannel(UserManagementService.Descriptor);
            return UserManagementService.NewClient(channel);
        }

        [Export]
        public static StatisticsService.StatisticsServiceClient CreateStatsClient(IChannelFactory channelFactory)
        {
            Channel channel = channelFactory.GetChannel(StatisticsService.Descriptor);
            return StatisticsService.NewClient(channel);
        }

        [Export]
        public static VideoCatalogService.VideoCatalogServiceClient CreateVideoClient(IChannelFactory channelFactory)
        {
            Channel channel = channelFactory.GetChannel(VideoCatalogService.Descriptor);
            return VideoCatalogService.NewClient(channel);
        }
    }
}
