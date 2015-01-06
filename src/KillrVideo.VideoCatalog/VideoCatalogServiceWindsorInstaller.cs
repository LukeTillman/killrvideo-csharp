using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using KillrVideo.Utils.Nimbus;
using KillrVideo.VideoCatalog.Messages.Events;

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

            // Messages published on the bus by service
            NimbusAssemblyConfig.AddFromTypes(typeof (YouTubeVideoAdded));
        }
    }
}
