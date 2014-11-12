using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Rebus;

namespace KillrVideo.UserManagement.Worker
{
    /// <summary>
    /// Castle Windsor installer for installing all the components needed by the UserManagement worker.
    /// </summary>
    public class UserManagementWindsorInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            // Register all message bus handlers in this assembly
            container.Register(Classes.FromThisAssembly().BasedOn<IHandleMessages>().WithServiceAllInterfaces().LifestyleTransient());

            // Register the UserManagement components as singletons so their state can be reused (prepared statements)
            container.Register(
                Classes.FromAssemblyContaining<UserWriteModel>().Pick()
                       .WithServiceFirstInterface().LifestyleSingleton());
        }
    }
}
