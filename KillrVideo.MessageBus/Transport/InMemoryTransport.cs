using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace KillrVideo.MessageBus.Transport
{
    /// <summary>
    /// An transport that does publish-subscribe in memory. Use the static Instance property to use
    /// so that all message bus instances running in process will share the same instance.
    /// </summary>
    public class InMemoryTransport : IMessageTransport
    {
        /// <summary>
        /// The static instance of the InMemoryTransport.
        /// </summary>
        public static InMemoryTransport Instance { get; }

        static InMemoryTransport()
        {
            Instance = new InMemoryTransport();
        }

        private readonly ConcurrentDictionary<string, InMemoryTopic> _topics;
        private readonly ConcurrentDictionary<string, string> _topicBySubscriptionId;

        /// <summary>
        /// Private constructor, use the Instance static property to get the Singleton instance.
        /// </summary>
        private InMemoryTransport()
        {
            _topics = new ConcurrentDictionary<string, InMemoryTopic>();
            _topicBySubscriptionId = new ConcurrentDictionary<string, string>();
        }
         
        public Task SendMessage(string topic, byte[] message, CancellationToken token)
        {
            InMemoryTopic inMemoryTopic = _topics.GetOrAdd(topic, t => new InMemoryTopic(t));
            return inMemoryTopic.Send(message, token);
        }

        public Task<string> Subscribe(string serviceName, string topic, CancellationToken token)
        {
            InMemoryTopic inMemoryTopic = _topics.GetOrAdd(topic, t => new InMemoryTopic(t));
            string id = inMemoryTopic.Subscribe(serviceName);

            // This shouldn't fail since each service should only subscribe once to a topic
            if (_topicBySubscriptionId.TryAdd(id, topic) == false)
                throw new InvalidOperationException($"Subscription for service {serviceName} already exists for topic {topic}");

            return Task.FromResult(id);
        }

        public Task<byte[]> ReceiveMessage(string subscription, CancellationToken token)
        {
            // Find the topic (should be found because subscriptions have to be created first)
            string topic;
            if (_topicBySubscriptionId.TryGetValue(subscription, out topic) == false)
                throw new InvalidOperationException($"Could not find topic name for subscription {subscription}");

            InMemoryTopic inMemoryTopic;
            if (_topics.TryGetValue(topic, out inMemoryTopic) == false)
                throw new InvalidOperationException($"Could not find in memory topic ${topic}");

            return inMemoryTopic.Receive(subscription, token);
        }
    }
}
