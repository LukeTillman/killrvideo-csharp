using System;
using System.Threading;
using System.Threading.Tasks;
using Castle.Windsor;
using KillrVideo.Utils.WorkerComposition;
using Nimbus;

namespace KillrVideo.Uploads.Worker
{
    /// <summary>
    /// The main entry point for the Uploads worker.
    /// </summary>
    public class WorkerRole : ILogicalWorkerRole
    {
        private readonly IWindsorContainer _container;
        private readonly CancellationTokenSource _cancelTokenSource;

        private Bus _bus;
        private Task _encodingListenerExecution;

        public WorkerRole(IWindsorContainer container)
        {
            if (container == null) throw new ArgumentNullException("container");
            _container = container;
            _cancelTokenSource = new CancellationTokenSource();
        }

        public Task OnStart()
        {
            // Make sure the bus is started
            _bus = _container.Resolve<Bus>();
            Task startBus = _bus.Start();

            // Execute the encoding listener job that we can cancel on stop
            _encodingListenerExecution = _container.Resolve<EncodingListenerJob>().Execute(_cancelTokenSource.Token);

            return startBus;
        }

        public Task OnStop()
        {
            // Cancel the encoding listener
            _cancelTokenSource.Cancel();

            // Stopped once the bus and the encoding listener job are stopped
            return Task.WhenAll(_bus.Stop(), _encodingListenerExecution);
        }
    }
}
