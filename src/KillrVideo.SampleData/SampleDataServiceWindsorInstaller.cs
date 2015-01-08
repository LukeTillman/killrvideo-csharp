using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;

namespace KillrVideo.SampleData
{
    /// <summary>
    /// Registers components needed by the Sample Data service with Windsor.
    /// </summary>
    public class SampleDataServiceWindsorInstaller : IWindsorInstaller
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
