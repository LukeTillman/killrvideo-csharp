using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using KillrVideo.UploadWorker.Jobs;
using Rebus;

namespace KillrVideo.Uploads.Worker
{
    /// <summary>
    /// Castle Windsor installer for installing all the components needed by the Uploads worker.
    /// </summary>
    public class UploadsWindsorInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            // Register all message bus handlers in this assembly
            container.Register(Classes.FromThisAssembly().BasedOn<IHandleMessages>().WithServiceAllInterfaces().LifestyleTransient());

            // Register the Uploads components as singletons so their state can be reused (prepared statements)
            container.Register(
                Classes.FromAssemblyContaining<UploadedVideosWriteModel>().Pick()
                       .WithServiceFirstInterface().LifestyleSingleton());

            // Register all IUploadWorkerJobs in this assembly
            container.Register(Classes.FromThisAssembly().BasedOn<IUploadWorkerJob>().WithServiceBase().LifestyleTransient());
        }
    }
}
