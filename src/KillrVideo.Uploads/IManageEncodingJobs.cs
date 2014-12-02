using System;
using System.Threading.Tasks;

namespace KillrVideo.Uploads
{
    /// <summary>
    /// Component for managing video encoding jobs.
    /// </summary>
    public interface IManageEncodingJobs
    {
        Task StartEncodingJob(Guid videoId, string uploadUrl);
    }
}