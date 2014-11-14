using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using KillrVideo.Statistics.Messages.Commands;
using KillrVideo.Utils.Nimbus;

namespace KillrVideo.Statistics.Worker
{
    /// <summary>
    /// Castle Windsor installer for installing all the components needed by the Statistics worker.
    /// </summary>
    public class StatisticsWindsorInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(
                Component.For<NimbusAssemblyConfig>()
                         .Instance(NimbusAssemblyConfig.FromTypes(typeof(StatisticsWindsorInstaller), typeof(RecordPlaybackStarted))));

            // Register the Statistics components as singletons so their state can be reused (prepared statements)
            container.Register(
                Classes.FromAssemblyContaining<PlaybackStatsWriteModel>().Pick()
                       .WithServiceFirstInterface().LifestyleSingleton());
        }
    }
}
