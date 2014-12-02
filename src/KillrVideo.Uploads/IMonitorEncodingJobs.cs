using System.Threading.Tasks;
using KillrVideo.Uploads.Dtos;

namespace KillrVideo.Uploads
{
    /// <summary>
    /// Component responsible for reacting to Azure Media Services encoding job events.
    /// </summary>
    public interface IMonitorEncodingJobs
    {
        /// <summary>
        /// Handles an event/notification from Azure Media Services about an encoding job.
        /// </summary>
        Task HandleEncodingJobEvent(EncodingJobEvent notification);
    }
}