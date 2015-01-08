using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;

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
        }
    }
}
