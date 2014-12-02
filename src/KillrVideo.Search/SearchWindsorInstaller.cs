using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using KillrVideo.Utils.Nimbus;

namespace KillrVideo.Search
{
    /// <summary>
    /// Castle Windsor installer for installing all the components needed by the Search worker.
    /// </summary>
    public class SearchWindsorInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(
                Component.For<NimbusAssemblyConfig>()
                         .Instance(NimbusAssemblyConfig.FromTypes(typeof(SearchWindsorInstaller))));
        }
    }
}
