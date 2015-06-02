using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;

namespace KillrVideo.Search
{
    /// <summary>
    /// Registers components needed by the video search service with Windsor.
    /// </summary>
    public class SearchServiceWindsorInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(
                // Most components
                // Classes.FromThisAssembly().Pick().WithServiceFirstInterface().LifestyleTransient()
                Component.For<ISearchVideos>().ImplementedBy<DataStaxEnterpriseSearch>().LifestyleTransient()
            );
        }
    }
}
