using System;
using System.Runtime.Remoting.Messaging;
using Castle.Facilities.TypedFactory;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using KillrVideo.Search.SearchImpl;

namespace KillrVideo.Search
{
    /// <summary>
    /// Registers components needed by the video search service with Windsor.
    /// </summary>
    public class SearchServiceWindsorInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(
                // Most components (except search implementations namespace)
                Classes.FromThisAssembly()
                       .Where(t => t.Namespace != typeof (SearchVideosByTag).Namespace)
                       .WithServiceFirstInterface()
                       .LifestyleTransient(),

                // Search implementations namespace
                Classes.FromThisAssembly()
                       .InSameNamespaceAs<SearchVideosByTag>()
                       .WithServiceSelf()
                       .LifestyleTransient()
                       .ConfigureFor<SearchComponentSelector>(c => c.LifestyleSingleton()),

                // Factory method for selecting a search service using the custom selector
                Component.For<Func<ISearchVideos>>().AsFactory(c => c.SelectedWith<SearchComponentSelector>()).LifestyleSingleton(),
                
                // When resolving the search service, use the factory method
                Component.For<ISearchVideos>().UsingFactoryMethod(k => k.Resolve<Func<ISearchVideos>>().Invoke()).LifestyleTransient()
            );
        }
    }
}
