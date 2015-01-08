using System.Threading;
using System.Threading.Tasks;

namespace KillrVideo.Utils.WorkerComposition
{
    /// <summary>
    /// Interface to represent logical workers which are housed inside of a physical worker role deployment and
    /// want to be started when the physical worker role is started.
    /// </summary>
    public interface ILogicalWorkerRole
    {
        /// <summary>
        /// Called when the physical worker role is starting.  The cancellation token will be used when stopping
        /// the physical worker role.
        /// </summary>
        Task OnStart(CancellationToken cancellationToken);
    }
}
