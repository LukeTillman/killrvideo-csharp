using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using KillrVideo.MessageBus.Transport;

namespace KillrVideo.MessageBus.Publish
{
    /// <summary>
    /// Component used to publish messages.
    /// </summary>
    internal class Publisher : IBus
    {
        private readonly IMessageTransport _transport;
        private readonly TaskCompletionSource<object> _started;

        private bool _stopped;

        public Publisher(IMessageTransport transport)
        {
            if (transport == null) throw new ArgumentNullException(nameof(transport));
            _transport = transport;

            _started = new TaskCompletionSource<object>();
            _stopped = false;
        }

        public Task Publish(IMessage message, CancellationToken token = default(CancellationToken))
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (_stopped) throw new InvalidOperationException("Publisher is stopped");

            // Do a fast path with no awaits once started
            return _started.Task.IsCompleted ? PublishImpl(message, token) : WaitForStartAndPublish(message, token);
        }

        /// <summary>
        /// Tell the publisher it's OK to start publishing messages.
        /// </summary>
        internal void Start()
        {
            _started.TrySetResult(null);
        }

        /// <summary>
        /// Tells the publisher to stop publishing messages.
        /// </summary>
        internal void Stop()
        {
            _started.TrySetCanceled();
            _stopped = true;
        }

        private Task PublishImpl(IMessage message, CancellationToken token)
        {
            string topic = message.Descriptor.FullName;
            return _transport.SendMessage(topic, message.ToByteArray(), token);
        }

        private async Task WaitForStartAndPublish(IMessage message, CancellationToken token)
        {
            // Wait for the bus to start
            await _started.Task.ConfigureAwait(false);

            // Publish once bus started (gives bus time to init subscriptions)
            await PublishImpl(message, token).ConfigureAwait(false);
        }
    }
}
