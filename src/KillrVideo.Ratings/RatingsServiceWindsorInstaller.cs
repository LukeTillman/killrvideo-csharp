using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;

namespace KillrVideo.Ratings
{
    /// <summary>
    /// Registers components needed by the video ratings service with Windsor.
    /// </summary>
    public class RatingsServiceWindsorInstaller : IWindsorInstaller
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
