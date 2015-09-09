using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using RestSharp;

namespace KillrVideo
{
    /// <summary>
    /// Register RestSharp components w/ Windsor.
    /// </summary>
    public class RestSharpWindsorInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(
                // RestSharp client
                Component.For<IRestClient>().ImplementedBy<RestClient>().LifestyleTransient()
            );
        }
    }
}