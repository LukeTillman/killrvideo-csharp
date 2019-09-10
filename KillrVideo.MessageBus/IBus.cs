using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;

namespace KillrVideo.MessageBus
{
    /// <summary>
    /// Component used to publish events.
    /// </summary>
    public interface IBus
    {
        /// <summary>
        /// Publishes an event to the bus for possible consumption by other services.
        /// </summary>
        Task Publish(IMessage message, CancellationToken token = default(CancellationToken));
    }
}
