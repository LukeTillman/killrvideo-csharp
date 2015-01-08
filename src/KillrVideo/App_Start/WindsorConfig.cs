using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Castle.Windsor.Installer;
using KillrVideo.Utils;
using KillrVideo.Utils.Configuration;

namespace KillrVideo
{
    /// <summary>
    /// Bootstrapper for the Castle Windsor IoC container.
    /// </summary>
    public static class WindsorConfig
    {
        /// <summary>
        /// Creates the Windsor container and does all necessary registrations for the KillrVideo app.
        /// </summary>
        public static IWindsorContainer CreateContainer()
        {
            // Create container
            var container = new WindsorContainer();

            // Add any app level registrations
            container.Register(Component.For<IGetEnvironmentConfiguration>().ImplementedBy<CloudConfigurationProvider>().LifestyleSingleton());
            
            // Add installers from this and all referenced app assemblies
            container.Install(FromAssembly.InThisApplication());
            return container;
        }
    }
}