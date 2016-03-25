using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using KillrVideo.MessageBus.Transport;
using Serilog;

namespace KillrVideo.MessageBus.Subscribe
{
    internal class SubscriptionServer
    {
        private static readonly ILogger Logger = Log.ForContext<SubscriptionServer>();

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
                    Logger.Error(ex, "Error while stopping subscription server");
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error while stopping subscription server");
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
                    Logger.Error(e, "Unexpected error while processing subscriptions");
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
                    Logger.Error(e, "Error while receiving from transport for subscription {subscription}", subscription.Id);

                    // Just start over with next message (TODO: Retry?)
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
                    Logger.Error(e, "Error while parsing message from transport for subscription {subscription}", subscription.Id);

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
                    Logger.Error(e, "Error dispatching to message handlers for subscription {subscription}", subscription.Id);

                    // Just start over with next message (TODO: Handler retries?)
                    continue;
                }

                processed = true;
            }

            return subscription;
        }
    }
}
