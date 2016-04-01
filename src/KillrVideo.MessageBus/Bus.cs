using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using KillrVideo.MessageBus.Publish;
using KillrVideo.MessageBus.Subscribe;
using Serilog;

namespace KillrVideo.MessageBus
{
    /// <summary>
    /// A bus that can be started/stopped. Use the IBus result from starting to publish messages.
    /// </summary>
    internal class Bus : IBusServer
    {
        private static readonly MethodInfo HandlerRegistrationFactoryMethod = typeof (HandlerRegistration).GetMethod("Create", BindingFlags.Static | BindingFlags.Public);
        private static readonly ILogger Logger = Log.ForContext<Bus>();

        private readonly CancellationTokenSource _cancelBusStart;
        private readonly List<HandlerRegistration> _handlers;

        private readonly SubscriptionServer _subscriptionServer;
        private readonly Publisher _publisher;

        private int _started;

        internal Bus(BusBuilder busConfig)
        {
            if (busConfig == null) throw new ArgumentNullException(nameof(busConfig));

            _cancelBusStart = new CancellationTokenSource();
            _handlers = new List<HandlerRegistration>();

            // Create components for the bus from config provided
            _subscriptionServer = new SubscriptionServer(busConfig.SerivceName, busConfig.Transport, busConfig.HandlerFactory);
            _publisher = new Publisher(busConfig.Transport);
        }

        /// <summary>
        /// Subscribes the specified handler Types.
        /// </summary>
        public void Subscribe(params Type[] handlerTypes)
        {
            if (_started != 0)
                throw new InvalidOperationException("Handlers can only be added before the bus is started");

            foreach (Type handlerType in handlerTypes)
            {
                // Try to get the IHandleMessage<T> interface from the handlerType
                var handlerInterfaces = handlerType.IsMessageHandlerInterface()
                                            ? new Type[] { handlerType }    // The handlerType itself is already IHandleMessage<T>
                                            : handlerType.GetMessageHandlerInterfaces().ToArray();    // The handlerType should implement IHandleMessage<T> at least once

                if (handlerInterfaces.Length == 0)
                    throw new InvalidOperationException($"The handler Type {handlerType.Name} is not a message handler");
                
                // Get the message Type T from IHandleMessage<T> and create a handler registration by invoking the static factory method
                // on HandlerRegistration with the appropriate type arguments
                foreach (Type handlerInterface in handlerInterfaces)
                {
                    Type messageType = handlerInterface.GenericTypeArguments.Single();
                    var handler =
                        (HandlerRegistration) HandlerRegistrationFactoryMethod.MakeGenericMethod(handlerType, messageType).Invoke(this, null);
                    _handlers.Add(handler);
                }
            }
        }

        /// <summary>
        /// Starts listening to any subscriptions that were added and returns an IBus instance that can publish messages.
        /// </summary>
        public void StartServer()
        {
            // Make sure bus is only started once
            int state = Interlocked.Increment(ref _started);
            if (state > 1)
                throw new InvalidOperationException("Bus can only be started once");

            // Start the subscription server and tell the publisher to start once subscriptions have been started
            _subscriptionServer.StartServer(_handlers, _cancelBusStart.Token)
                               .ContinueWith(t =>
                               {
                                   if (!t.IsCanceled && !t.IsFaulted)
                                       _publisher.Start();
                               }, _cancelBusStart.Token);

        }

        /// <summary>
        /// Stops any subscriptions that were added.
        /// </summary>
        public async Task StopServerAsync()
        {
            // Make sure bus is only stopped once
            int state = Interlocked.Increment(ref _started);
            if (state == 1)
                throw new InvalidOperationException("Bus was never started");

            if (state > 2)
                throw new InvalidOperationException("Bus cannot be stopped multiple times");

            // Tell the publisher to stop publishing new events
            _publisher.Stop();

            // Stop the subscriptions
            try
            {
                // Cancel in case the Start Task is still running
                _cancelBusStart.Cancel();
            }
            catch (AggregateException e)
            {
                foreach (var ex in e.IgnoreTaskCanceled())
                    Logger.Error(ex, "Error while stopping server");
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error while stopping server");
            }

            // Wait for the subsciption server to stop
            await _subscriptionServer.StopServer().ConfigureAwait(false);
        }
        
        public Task Publish(IMessage message, CancellationToken token = new CancellationToken())
        {
            return _publisher.Publish(message, token);
        }
    }
}