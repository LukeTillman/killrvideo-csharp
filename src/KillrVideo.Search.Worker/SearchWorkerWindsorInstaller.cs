using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using KillrVideo.Utils.Nimbus;
using KillrVideo.VideoCatalog.Messages.Events;

namespace KillrVideo.Search.Worker
{
    /// <summary>
    /// Windsor installer for installing components needed by the Search service worker.
    /// </summary>
    public class SearchWorkerWindsorInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            // Register Nimbus handler/message assemblies
            NimbusAssemblyConfig.AddFromTypes(typeof (SearchWorkerWindsorInstaller), typeof (IVideoAdded));
        }
    }
}
