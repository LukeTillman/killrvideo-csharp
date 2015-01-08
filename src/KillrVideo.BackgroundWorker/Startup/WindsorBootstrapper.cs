using Castle.MicroKernel.Registration;
using Castle.MicroKernel.Resolvers.SpecializedResolvers;
using Castle.Windsor;
using Castle.Windsor.Installer;
using KillrVideo.BackgroundWorker.Utils;
using KillrVideo.Utils.Configuration;

namespace KillrVideo.BackgroundWorker.Startup
{
    /// <summary>
    /// Bootstrapping class for Castle Windsor IoC container.
    /// </summary>
    public static class WindsorBootstrapper
    {
        /// <summary>
        /// Creates the Windsor container and does all necessary registrations for the worker role.
        /// </summary>
        public static IWindsorContainer CreateContainer()
        {
            // Create container and allow collections to be resolved
            var container = new WindsorContainer();
            container.Kernel.Resolver.AddSubResolver(new CollectionResolver(container.Kernel));

            // Add any app level registrations
            container.Register(Component.For<IGetEnvironmentConfiguration>().ImplementedBy<CloudConfigurationProvider>().LifestyleSingleton());

            // Add installers from this and all referenced app assemblies
            container.Install(FromAssembly.InThisApplication());
            return container;
        }
    }
}
