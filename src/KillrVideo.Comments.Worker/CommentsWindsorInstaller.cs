using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using KillrVideo.Comments.Messages.Commands;
using KillrVideo.Utils.Nimbus;

namespace KillrVideo.Comments.Worker
{
    /// <summary>
    /// Castle Windsor installer for installing all the components needed by the Comments worker.
    /// </summary>
    public class CommentsWindsorInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            // Tell Nimbus to scan this assembly and the corresponsing messages assembly
            container.Register(
                Component.For<NimbusAssemblyConfig>()
                         .Instance(NimbusAssemblyConfig.FromTypes(typeof (CommentsWindsorInstaller), typeof (CommentOnVideo))));

            // Register the Comments components as singletons so their state can be reused (prepared statements)
            container.Register(
                Classes.FromAssemblyContaining<CommentWriteModel>().Pick()
                       .WithServiceFirstInterface().LifestyleSingleton());
        }
    }
}
