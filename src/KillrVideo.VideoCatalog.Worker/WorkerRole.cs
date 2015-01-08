using System;
using System.Threading;
using System.Threading.Tasks;
using Castle.Windsor;
using KillrVideo.Utils.WorkerComposition;
using Nimbus;

namespace KillrVideo.VideoCatalog.Worker
{
    /// <summary>
    /// The main entry point for the VideoCatalog worker.
    /// </summary>
    public class WorkerRole : ILogicalWorkerRole
    {
        private readonly IWindsorContainer _container;

        public WorkerRole(IWindsorContainer container)
        {
            if (container == null) throw new ArgumentNullException("container");
            _container = container;
        }
        
        public Task OnStart(CancellationToken cancellationToken)
        {
            // Make sure the bus is started
            var bus = _container.Resolve<Bus>();
            bus.Start();

            // Just return a completed Task since we don't have anything else to do
            return Task.Delay(0, cancellationToken);
        }
    }
}
