using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Rebus;

namespace KillrVideo.Statistics.Worker
{
    /// <summary>
    /// Castle Windsor installer for installing all the components needed by the Statistics worker.
    /// </summary>
    public class StatisticsWindsorInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            // Register all message bus handlers in this assembly
            container.Register(Classes.FromThisAssembly().BasedOn<IHandleMessages>().WithServiceAllInterfaces().LifestyleTransient());

            // Register the Statistics components as singletons so their state can be reused (prepared statements)
            container.Register(
                Classes.FromAssemblyContaining<PlaybackStatsWriteModel>().Pick()
                       .WithServiceFirstInterface().LifestyleSingleton());
        }
    }
}
