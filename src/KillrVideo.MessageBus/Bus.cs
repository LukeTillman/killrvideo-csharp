using System;
using System.Threading;
using System.Threading.Tasks;
using DryIocAttributes;
using Google.Protobuf;
using KillrVideo.Host;
using KillrVideo.Host.Config;
using KillrVideo.Host.Tasks;
using KillrVideo.MessageBus.Publish;
using KillrVideo.MessageBus.Subscribe;
using KillrVideo.MessageBus.Transport;
using Serilog;

namespace KillrVideo.MessageBus
{
    /// <summary>
    /// A bus that can be started/stopped. Use the IBus result from starting to publish messages.
    /// </summary>
    [ExportMany]
    public class Bus : IHostTask, IBus
    {
        private static readonly ILogger Logger = Log.ForContext<Bus>();

        private readonly CancellationTokenSource _cancelBusStart;
        private readonly SubscriptionServer _subscriptionServer;
        private readonly Publisher _publisher;

        private Task _startedServer;
        private int _started;

        /// <summary>
        /// The name of the task.
        /// </summary>
        public string Name => "Message Bus Server";

        public Bus(IHostConfiguration hostConfig, IMessageTransport messageTransport, IHandlerFactory handlerFactory)
        {
            if (hostConfig == null) throw new ArgumentNullException(nameof(hostConfig));
            if (messageTransport == null) throw new ArgumentNullException(nameof(messageTransport));
            if (handlerFactory == null) throw new ArgumentNullException(nameof(handlerFactory));

            _cancelBusStart = new CancellationTokenSource();

            // Create components for the bus from config provided
            _subscriptionServer = new SubscriptionServer(hostConfig.ApplicationName, messageTransport, handlerFactory);
            _publisher = new Publisher(messageTransport);
            _startedServer = Task.CompletedTask;
        }

        /// <summary>
        /// Starts processing any subscriptions that are available.
        /// </summary>
        public void Start()
        {
            // Make sure bus is only started once
            int state = Interlocked.Increment(ref _started);
            if (state > 1)
                throw new InvalidOperationException("Bus can only be started once");

            // Start the subscription server and tell the publisher to start once subscriptions have been started
            _startedServer = StartImpl(_cancelBusStart.Token);
        }

        private async Task StartImpl(CancellationToken token)
        {
            bool started = false;
            while (true)
            {
                token.ThrowIfCancellationRequested();

                // Start the subscription server
                try
                {
                    Logger.Information("Starting message bus subscription handlers");
                    await _subscriptionServer.StartServer(token).ConfigureAwait(false);
                    Logger.Information("Started subscription handlers, starting publisher");
                    _publisher.Start();
                    Logger.Information("Started publisher");
                    started = true;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Unexpected exception while starting message bus");
                }

                // If started, good to go
                if (started)
                    break;

                // Wait and retry
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Unexpected exception while waiting to retry starting server");
                }
            }
        }

        /// <summary>
        /// Stops any subscriptions that were added.
        /// </summary>
        public async Task StopAsync()
        {
            // Make sure bus is only stopped once
            int state = Interlocked.Increment(ref _started);
            if (state == 1)
                throw new InvalidOperationException("Bus was never started");

            if (state > 2)
                throw new InvalidOperationException("Bus cannot be stopped multiple times");

            Logger.Information("Stopping message bus publisher and subscription handlers");

            // Tell the publisher to stop publishing new events
            _publisher.Stop();

            // Stop the subscriptions
            try
            {
                // Cancel in case the Start Task is still running
                _cancelBusStart.Cancel();
                await _startedServer.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
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

            Logger.Information("Stopped message bus publisher and subscription handlers");
        }
        
        public Task Publish(IMessage message, CancellationToken token = new CancellationToken())
        {
            return _publisher.Publish(message, token);
        }
    }
}