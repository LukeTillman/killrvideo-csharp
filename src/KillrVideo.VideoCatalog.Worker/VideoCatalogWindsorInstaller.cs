using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using KillrVideo.Utils.Nimbus;
using KillrVideo.VideoCatalog.Messages.Commands;

namespace KillrVideo.VideoCatalog.Worker
{
    /// <summary>
    /// Castle Windsor installer for installing all the components needed by the VideoCatalog worker.
    /// </summary>
    public class VideoCatalogWindsorInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(
                Component.For<NimbusAssemblyConfig>()
                         .Instance(NimbusAssemblyConfig.FromTypes(typeof(VideoCatalogWindsorInstaller), typeof(SubmitUploadedVideo))));

            // Register the VideoCatalog components as singletons so their state can be reused (prepared statements)
            container.Register(
                Classes.FromAssemblyContaining<VideoCatalogWriteModel>().Pick()
                       .WithServiceFirstInterface().LifestyleSingleton());
        }
    }
}
