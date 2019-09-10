using System.Threading.Tasks;

namespace KillrVideo.Host.Tasks
{
    /// <summary>
    /// A task to run on the host that can be started/stopped.
    /// </summary>
    public interface IHostTask
    {
        /// <summary>
        /// The name of the task.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Starts the background task.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the background task.
        /// </summary>
        Task StopAsync();
    }
}
