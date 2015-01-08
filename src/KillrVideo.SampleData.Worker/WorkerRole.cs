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
        private readonly IWindsorContainer _windsorContainer;

        public WorkerRole(IWindsorContainer windsorContainer)
        {
            if (windsorContainer == null) throw new ArgumentNullException("windsorContainer");
            _windsorContainer = windsorContainer;
        }
        
        public Task OnStart(CancellationToken cancellationToken)
        {
            // Make sure the bus is started
            var bus = _windsorContainer.Resolve<Bus>();
            bus.Start();

            // Start the sample data scheduler and return it
            return _windsorContainer.Resolve<SampleDataJobScheduler>().Run(cancellationToken);
        }
    }
}
