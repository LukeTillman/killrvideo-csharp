using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Rebus;

namespace KillrVideo.Ratings.Worker
{
    /// <summary>
    /// Castle Windsor installer for installing all the components needed by the Ratings worker.
    /// </summary>
    public class RatingsWindsorInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            // Register all message bus handlers in this assembly
            container.Register(Classes.FromThisAssembly().BasedOn<IHandleMessages>().WithServiceAllInterfaces().LifestyleTransient());

            // Register the Ratings components as singletons so their state can be reused (prepared statements)
            container.Register(
                Classes.FromAssemblyContaining<RatingsWriteModel>().Pick()
                       .WithServiceFirstInterface().LifestyleSingleton());
        }
    }
}
