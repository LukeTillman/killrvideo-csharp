using System.Threading;
using System.Threading.Tasks;

namespace KillrVideo.MessageBus.Transport
{
    /// <summary>
    /// Common interface for a message transport, used for sending/receiving messages from topics.
    /// </summary>
    public interface IMessageTransport
    {
        /// <summary>
        /// Sends a message payload to the topic specified.
        /// </summary>
        Task SendMessage(string topic, byte[] message, CancellationToken token);

        /// <summary>
        /// Subscribes a service to the specified topic and returns a unique identifier for the subscription that
        /// can be used in calls to ReceiveMessage.
        /// </summary>
        Task<string> Subscribe(string serviceName, string topic, CancellationToken token);

        /// <summary>
        /// Receives a message from a topic.
        /// </summary>
        Task<byte[]> ReceiveMessage(string subscription, CancellationToken token);
    }
}