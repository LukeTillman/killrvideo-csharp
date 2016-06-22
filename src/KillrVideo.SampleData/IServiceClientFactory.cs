using System.Threading.Tasks;
using KillrVideo.Comments;
using KillrVideo.Ratings;
using KillrVideo.Statistics;
using KillrVideo.UserManagement;
using KillrVideo.VideoCatalog;

namespace KillrVideo.SampleData
{
    /// <summary>
    /// Factory for getting service clients in the Sample Data service.
    /// </summary>
    public interface IServiceClientFactory
    {
        Task<CommentsService.CommentsServiceClient> GetCommentsClientAsync();
        Task<RatingsService.RatingsServiceClient> GetRatingsClientAsync();
        Task<StatisticsService.StatisticsServiceClient> GetStatsClientAsync();
        Task<UserManagementService.UserManagementServiceClient> GetUsersClientAsync();
        Task<VideoCatalogService.VideoCatalogServiceClient> GetVideoClientAsync();
    }
}