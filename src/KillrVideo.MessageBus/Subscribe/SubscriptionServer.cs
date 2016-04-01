using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KillrVideo.MessageBus.Transport;
using Serilog;

namespace KillrVideo.MessageBus.Subscribe
{
    internal class SubscriptionServer
    {
        private static readonly ILogger Logger = Log.ForContext<SubscriptionServer>();

        private readonly string _serviceName;
        private readonly IMessageTransport _transport;
        private readonly IHandlerFactory _handlerFactory;

        private readonly CancellationTokenSource _cancelRunningServer;
        private Task _runningServer;

        public SubscriptionServer(string serviceName, IMessageTransport transport, IHandlerFactory handlerFactory)
        {
            if (serviceName == null) throw new ArgumentNullException(nameof(serviceName));
            if (transport == null) throw new ArgumentNullException(nameof(transport));
            if (handlerFactory == null) throw new ArgumentNullException(nameof(handlerFactory));
            _serviceName = serviceName;
            _transport = transport;
            _handlerFactory = handlerFactory;

            _cancelRunningServer = new CancellationTokenSource();
            _runningServer = Task.CompletedTask;
        }

        public async Task StartServer(List<HandlerRegistration> handlers, CancellationToken token = default(CancellationToken))
        {
            // Nothing to do if no handlers
            if (handlers.Count == 0)
                return;

            // Get the unique message FullNames amongst the handlers and use those as the topic name to subscribe to
            Dictionary<string, List<HandlerRegistration>> handlersByTopic = handlers.GroupBy(h => h.MessageDescriptor.FullName)
                                                                                    .ToDictionary(g => g.Key, g => g.ToList());

            // Subscribe to all topics
            Subscription[] subscriptions = await Task.WhenAll(handlersByTopic.Select(kvp => Subscribe(kvp.Key, kvp.Value, token)))
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

        private async Task<Subscription> Subscribe(string topic, List<HandlerRegistration> handlers, CancellationToken token)
        {
            string id = await _transport.Subscribe(_serviceName, topic, token).ConfigureAwait(false);
            return new Subscription(id, handlers);
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

                // Dispatch to handlers
                try
                {
                    var dispatchTasks = subscription.Handlers.Select(h => h.Dispatch(_handlerFactory, msgBytes));
                    await Task.WhenAll(dispatchTasks).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Error while dispatching message to handlers for subscription {subscription}", subscription.Id);

                    // Just go to next message (TODO: Handler retries?)
                    continue;
                }
                
                processed = true;
            }

            return subscription;
        }
    }
}
