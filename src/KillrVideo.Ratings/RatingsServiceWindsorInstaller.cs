using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using KillrVideo.Ratings.Messages.Events;
using KillrVideo.Utils.Nimbus;

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

            // Messages published on the bus by service
            NimbusAssemblyConfig.AddFromTypes(typeof (UserRatedVideo));
        }
    }
}
