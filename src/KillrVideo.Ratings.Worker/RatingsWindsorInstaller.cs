using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using KillrVideo.Ratings.Messages.Commands;
using KillrVideo.Utils.Nimbus;

namespace KillrVideo.Ratings.Worker
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

            // Register the Ratings components as singletons so their state can be reused (prepared statements)
            container.Register(
                Classes.FromAssemblyContaining<RatingsWriteModel>().Pick()
                       .WithServiceFirstInterface().LifestyleSingleton());
        }
    }
}
