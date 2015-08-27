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
        /// Called when the physical worker role is starting.
        /// </summary>
        Task OnStart();

        /// <summary>
        /// Called when the physical worker role is stopping.
        /// </summary>
        Task OnStop();
    }
}
