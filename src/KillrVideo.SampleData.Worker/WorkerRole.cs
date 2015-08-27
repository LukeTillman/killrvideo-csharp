using System;
using System.Threading;
using System.Threading.Tasks;
using Castle.Windsor;
using KillrVideo.SampleData.Worker.Scheduler;
using KillrVideo.Utils.WorkerComposition;
using Nimbus;

namespace KillrVideo.SampleData.Worker
{
    /// <summary>
    /// Main entry point for the SampleData worker.
    /// </summary>
    public class WorkerRole : ILogicalWorkerRole
    {
        private readonly IWindsorContainer _container;
        private readonly CancellationTokenSource _cancelTokenSource;

        private Bus _bus;
        private Task _schedulerExecution;

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

            // Execute the scheduler that we can cancel on stop
            _schedulerExecution = _container.Resolve<SampleDataJobScheduler>().Run(_cancelTokenSource.Token);

            return startBus;
        }

        public Task OnStop()
        {
            // Cancel the scheduler
            _cancelTokenSource.Cancel();

            // Stopped once the bus and the scheduler are stopped
            return Task.WhenAll(_bus.Stop(), _schedulerExecution);
        }
    }
}
