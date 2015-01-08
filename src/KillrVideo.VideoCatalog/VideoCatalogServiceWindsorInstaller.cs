using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;

namespace KillrVideo.VideoCatalog
{
    /// <summary>
    /// Registers all components needed by the video catalog service with Windsor.
    /// </summary>
    public class VideoCatalogServiceWindsorInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(
                // Most components
                Classes.FromThisAssembly().Pick().WithServiceFirstInterface().LifestyleTransient()
            );
        }
    }
}
