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

            // Start the encoding job monitor and return it
            return _container.Resolve<EncodingListenerJob>().Execute(cancellationToken);
        }
    }
}
