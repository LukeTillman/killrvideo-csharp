using System.Threading;
using System.Threading.Tasks;

namespace KillrVideo.Uploads.Worker.Jobs
{
    /// <summary>
    /// Interface for a "job" that should be run in this upload worker.
    /// </summary>
    public interface IUploadWorkerJob
    {
        /// <summary>
        /// Executes the job.
        /// </summary>
        Task Execute(CancellationToken cancellationToken);
    }
}
