using System;
using System.Threading;
using System.Threading.Tasks;
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
        private static readonly ILogger Logger = Log.ForContext<Bus>();

        private readonly CancellationTokenSource _cancelBusStart;

        private readonly SubscriptionServer _subscriptionServer;
        private readonly Publisher _publisher;

        private int _started;

        internal Bus(BusBuilder busConfig)
        {
            if (busConfig == null) throw new ArgumentNullException(nameof(busConfig));

            _cancelBusStart = new CancellationTokenSource();

            // Create components for the bus from config provided
            _subscriptionServer = new SubscriptionServer(busConfig.SerivceName, busConfig.Transport, busConfig.Handlers);
            _publisher = new Publisher(busConfig.Transport);
        }
        
        /// <summary>
        /// Starts listening to any subscriptions that were added and returns an IBus instance that can publish messages.
        /// </summary>
        public IBus StartServer()
        {
            // Make sure bus is only started once
            int state = Interlocked.Increment(ref _started);
            if (state > 1)
                throw new InvalidOperationException("Bus can only be started once");

            // Start the subscription server and tell the publisher to start once subscriptions have been started
            _subscriptionServer.StartServer(_cancelBusStart.Token)
                               .ContinueWith(t =>
                               {
                                   if (!t.IsCanceled && !t.IsFaulted)
                                       _publisher.Start();
                               }, _cancelBusStart.Token);

            return _publisher;
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

        
    }
}