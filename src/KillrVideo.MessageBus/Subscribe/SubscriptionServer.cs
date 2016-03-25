using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using KillrVideo.MessageBus.Transport;

namespace KillrVideo.MessageBus.Subscribe
{
    internal class SubscriptionServer
    {
        private readonly string _serviceName;
        private readonly IMessageTransport _transport;
        private readonly IDictionary<MessageDescriptor, List<IHandlerAdapter>> _handlers;

        private readonly CancellationTokenSource _cancelRunningServer;
        private Task _runningServer;

        public SubscriptionServer(string serviceName, IMessageTransport transport, IDictionary<MessageDescriptor, List<IHandlerAdapter>> handlers)
        {
            if (serviceName == null) throw new ArgumentNullException(nameof(serviceName));
            if (transport == null) throw new ArgumentNullException(nameof(transport));
            if (handlers == null) throw new ArgumentNullException(nameof(handlers));

            _serviceName = serviceName;
            _transport = transport;
            _handlers = handlers;

            _cancelRunningServer = new CancellationTokenSource();
            _runningServer = Task.CompletedTask;
        }

        public async Task StartServer(CancellationToken token = default(CancellationToken))
        {
            // Nothing to do if no handlers
            if (_handlers.Count == 0)
                return;

            // Subscribe to all topics
            Subscription[] subscriptions = await Task.WhenAll(_handlers.Select(kvp => Subscribe(kvp.Key, kvp.Value, token)))
                                                     .ConfigureAwait(false);

            // Kick off a background task to pull messages off the transport for each subscription and dispatch the messages
            _runningServer = Task.Run(() => RunServer(subscriptions, _cancelRunningServer.Token), _cancelRunningServer.Token);
        }

        /// <summary>
        /// Stops the subscription server processing messages. Should not throw an Exception.
        /// </summary>
        public async Task StopServer()
        {
            try
            {
                _cancelRunningServer.Cancel();
                await _runningServer.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected, no-op
            }
            catch (AggregateException e)
            {
                foreach (var ex in e.IgnoreTaskCanceled())
                {
                    Console.WriteLine("Error while stopping subscription server");
                    Console.WriteLine(ex.ToString());
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error while stopping subscription server");
                Console.WriteLine(e.ToString());
            }
        }

        private async Task<Subscription> Subscribe(MessageDescriptor messageType, List<IHandlerAdapter> handlers, CancellationToken token)
        {
            string topic = messageType.FullName;
            string id = await _transport.Subscribe(_serviceName, topic, token).ConfigureAwait(false);
            return new Subscription(id, messageType, handlers);
        }

        private async Task RunServer(Subscription[] subscriptions, CancellationToken token)
        {
            // Create a dictionary of message processing tasks by subscription id
            Dictionary<string, Task<Subscription>> tasks = subscriptions.ToDictionary(s => s.Id, s => ProcessNextMessage(s, token));

            while (true)
            {
                // Cancel if necessary
                token.ThrowIfCancellationRequested();

                try
                {
                    // Wait for a processing task to complete
                    Task<Subscription> completedTask = await Task.WhenAny(tasks.Values).ConfigureAwait(false);

                    // ProcessNextMessage operation should not throw an Exception, but it could have been cancelled so check
                    token.ThrowIfCancellationRequested();

                    // Replace the completed task in the dictionary with a new processing task
                    tasks[completedTask.Result.Id] = ProcessNextMessage(completedTask.Result, token);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error while processing subscriptions");
                    Console.WriteLine(e.ToString());
                }
            }
        }

        /// <summary>
        /// Process the next message for the subscription and return the subscription when successful.
        /// </summary>
        private async Task<Subscription> ProcessNextMessage(Subscription subscription, CancellationToken token)
        {
            bool processed = false;

            while (!processed)
            {
                token.ThrowIfCancellationRequested();

                // Get the message from the transport
                byte[] msgBytes;
                try
                {
                    msgBytes = await _transport.ReceiveMessage(subscription.Id, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error while receiving from transport");
                    Console.WriteLine(e.ToString());

                    // Just start over with next message
                    continue;
                }

                // Parse the Protobuf bytes
                IMessage message;
                try
                {
                    message = subscription.MessageType.Parser.ParseFrom(msgBytes);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error parsing message from transport");
                    Console.WriteLine(e.ToString());

                    // Just start over with next message
                    continue;
                }

                // Dispatch the message to handlers
                try
                {
                    await subscription.DispatchMessage(message).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error dispatching to message handlers");
                    Console.WriteLine(e.ToString());

                    // Just start over with next message
                    continue;
                }

                processed = true;
            }

            return subscription;
        }
    }
}
