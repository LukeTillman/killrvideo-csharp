using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KillrVideo.MessageBus.Transport
{
    internal class InMemoryTopic
    {
        private readonly string _topic;
        private readonly ConcurrentDictionary<string, AsyncQueue<byte[]>> _subscriptions;

        public InMemoryTopic(string topic)
        {
            _topic = topic;
            _subscriptions = new ConcurrentDictionary<string, AsyncQueue<byte[]>>();
        }

        public string Subscribe(string serviceName)
        {
            string id = $"{serviceName}:{_topic}";

            // This shouldn't fail since subscriptions should be unique and each service should only subscribe once
            if (_subscriptions.TryAdd(id, new AsyncQueue<byte[]>(50)) == false)
                throw new Exception("Failed to add subscription");

            return id;
        }

        public Task Send(byte[] message, CancellationToken token)
        {
            // Send is complete when pushed to all subscription queues
            return Task.WhenAll(_subscriptions.Values.Select(queue => queue.Enqueue(message, token)));
        }

        public Task<byte[]> Receive(string subscriptionId, CancellationToken token)
        {
            // Find the subscription and dequeue a message
            AsyncQueue<byte[]> queue;
            if (_subscriptions.TryGetValue(subscriptionId, out queue) == false)
                throw new InvalidOperationException($"Subscription {subscriptionId} not found in topic {_topic}");

            return queue.Dequeue(token);
        }
    }
}