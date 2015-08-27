using System;
using System.Threading.Tasks;
using Castle.Windsor;
using KillrVideo.Utils.WorkerComposition;
using Nimbus;

namespace KillrVideo.Search.Worker
{
    /// <summary>
    /// Main entry point for the Search worker.
    /// </summary>
    public class WorkerRole : ILogicalWorkerRole
    {
        private readonly IWindsorContainer _container;
        private Bus _bus;

        public WorkerRole(IWindsorContainer container)
        {
            if (container == null) throw new ArgumentNullException("container");
            _container = container;
        }

        public Task OnStart()
        {
            // Make sure the bus is started
            _bus = _container.Resolve<Bus>();
            return _bus.Start();
        }

        public Task OnStop()
        {
            return _bus.Stop();
        }
    }
}
