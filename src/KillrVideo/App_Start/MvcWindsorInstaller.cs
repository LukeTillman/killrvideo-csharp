using System.Web.Mvc;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;

namespace KillrVideo
{
    /// <summary>
    /// Registers all the ASP.NET MVC components (controllers for example) with the container.
    /// </summary>
    public class MvcWindsorInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            // Register all MVC controllers in this assembly with the container
            container.Register(
                Classes.FromThisAssembly().BasedOn<IController>().LifestyleTransient()
            );
        }
    }
}