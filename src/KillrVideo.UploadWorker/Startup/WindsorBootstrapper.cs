using Castle.MicroKernel.Registration;
using Castle.Windsor;
using KillrVideo.UploadWorker.Jobs;

namespace KillrVideo.UploadWorker.Startup
{
    /// <summary>
    /// Bootstrapping class for Castle Windsor IoC container.
    /// </summary>
    public static class WindsorBootstrapper
    {
        /// <summary>
        /// Creates a new Windsor container with all appropriate registrations.
        /// </summary>
        public static WindsorContainer CreateContainer()
        {
            var container = new WindsorContainer();
            RegisterJobs(container);
            return container;
        }

        /// <summary>
        /// Registers all IUploadWorkerJob implementations with the container.
        /// </summary>
        private static void RegisterJobs(WindsorContainer container)
        {
            container.Register(
                Classes.FromThisAssembly().BasedOn<IUploadWorkerJob>().WithServiceBase().LifestyleTransient()
            );
        }

    }
}
