using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;

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
                Classes.FromThisAssembly().Pick().WithServiceFirstInterface().LifestyleTransient()
            );
        }
    }
}
