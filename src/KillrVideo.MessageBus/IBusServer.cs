using System.Threading.Tasks;

namespace KillrVideo.MessageBus
{
    /// <summary>
    /// A bus server that can be started/stopped.
    /// </summary>
    public interface IBusServer
    {
        /// <summary>
        /// Starts the server processing any subscriptions and returns a bus that can
        /// publish events.
        /// </summary>
        IBus StartServer();

        /// <summary>
        /// Stops any subscriptions and returns a Task that will complete once the server
        /// is shutdown.
        /// </summary>
        Task StopServerAsync();
    }
}
