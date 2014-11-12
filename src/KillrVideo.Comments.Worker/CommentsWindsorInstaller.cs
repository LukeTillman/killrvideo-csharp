using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Rebus;

namespace KillrVideo.Comments.Worker
{
    /// <summary>
    /// Castle Windsor installer for installing all the components needed by the Comments worker.
    /// </summary>
    public class CommentsWindsorInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            // Register all message bus handlers in this assembly
            container.Register(Classes.FromThisAssembly().BasedOn<IHandleMessages>().WithServiceAllInterfaces().LifestyleTransient());

            // Register the Comments components as singletons so their state can be reused (prepared statements)
            container.Register(
                Classes.FromAssemblyContaining<CommentWriteModel>().Pick()
                       .WithServiceFirstInterface().LifestyleSingleton());
        }
    }
}
