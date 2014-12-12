using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using KillrVideo.Uploads.Dtos;
using KillrVideo.Utils.Nimbus;

namespace KillrVideo.Uploads
{
    /// <summary>
    /// Registers all components needed by the video uploads service with Windsor.
    /// </summary>
    public class UploadsServiceWindsorInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(
                // Most components
                Classes.FromThisAssembly().Pick().WithServiceFirstInterface().LifestyleTransient(),

                // Messages sent on the bus by service (doesn't currently publish any events, those are handled in the Worker)
                Component.For<NimbusAssemblyConfig>().Instance(NimbusAssemblyConfig.FromTypes(typeof(GenerateUploadDestination)))
            );
        }
    }
}
