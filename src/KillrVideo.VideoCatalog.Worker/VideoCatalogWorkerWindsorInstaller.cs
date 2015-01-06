using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using KillrVideo.Uploads.Messages.Events;
using KillrVideo.Utils.Nimbus;
using KillrVideo.VideoCatalog.Messages.Events;

namespace KillrVideo.VideoCatalog.Worker
{
    /// <summary>
    /// Windsor installer for the video catalog worker components.
    /// </summary>
    public class VideoCatalogWorkerWindsorInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            // Register Nimbus handler/message assemblies
            NimbusAssemblyConfig.AddFromTypes(typeof (VideoCatalogWorkerWindsorInstaller), typeof (UploadedVideoPublished),
                                              typeof (UploadedVideoAdded));
        }
    }
}
