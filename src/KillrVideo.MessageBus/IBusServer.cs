using System;
using System.Threading.Tasks;

namespace KillrVideo.MessageBus
{
    /// <summary>
    /// A bus server that can be started/stopped.
    /// </summary>
    public interface IBusServer : IBus
    {
        /// <summary>
        /// Subscribes the specified handler Types.
        /// </summary>
        void Subscribe(params Type[] handlerTypes);

        /// <summary>
        /// Starts the server processing any subscriptions.
        /// </summary>
        void StartServer();

        /// <summary>
        /// Stops any subscriptions and returns a Task that will complete once the server is shutdown.
        /// </summary>
        Task StopServerAsync();
    }
}
