using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using KillrVideo.Ratings.Messages.Commands;
using KillrVideo.Utils.Nimbus;

namespace KillrVideo.Ratings
{
    /// <summary>
    /// Castle Windsor installer for installing all the components needed by the Ratings worker.
    /// </summary>
    public class RatingsWindsorInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(
                Component.For<NimbusAssemblyConfig>()
                         .Instance(NimbusAssemblyConfig.FromTypes(typeof(RatingsWindsorInstaller), typeof(RateVideo))));
        }
    }
}
