using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using KillrVideo.Comments.Messages.Events;
using KillrVideo.Utils.Nimbus;

namespace KillrVideo.Comments
{
    /// <summary>
    /// Registers components needed by the Comments service with Windsor.
    /// </summary>
    public class CommentsServiceWindsorInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(
                // Most components
                Classes.FromThisAssembly().Pick().WithServiceFirstInterface().LifestyleTransient()
            );

            // Messages published on the bus by service
            NimbusAssemblyConfig.AddFromTypes(typeof (UserCommentedOnVideo));
        }
    }
}
