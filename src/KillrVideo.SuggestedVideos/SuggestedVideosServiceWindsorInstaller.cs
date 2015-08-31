using System;
using Castle.Facilities.TypedFactory;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using KillrVideo.SuggestedVideos.SuggestionImpl;

namespace KillrVideo.SuggestedVideos
{
    /// <summary>
    /// Registers all components needed by the suggested videos service with Windsor.
    /// </summary>
    public class SuggestedVideosServiceWindsorInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(
                // Most components (except suggested videos implementations namespace)
                Classes.FromThisAssembly()
                       .Where(t => t.Namespace != typeof (SuggestVideosByTag).Namespace)
                       .WithServiceFirstInterface()
                       .LifestyleTransient(),

                // Suggested videos implementations namespace
                Classes.FromThisAssembly()
                       .InSameNamespaceAs<SuggestVideosByTag>()
                       .WithServiceSelf()
                       .LifestyleTransient()
                       .ConfigureFor<SuggestedVideosComponentSelector>(c => c.LifestyleSingleton()),

                // Factory method for selecting a suggested videos service using the custom selector
                Component.For<Func<ISuggestVideos>>().AsFactory(c => c.SelectedWith<SuggestedVideosComponentSelector>()).LifestyleSingleton(),

                // When resolving the search service, use the factory method
                Component.For<ISuggestVideos>().UsingFactoryMethod(k => k.Resolve<Func<ISuggestVideos>>().Invoke()).LifestyleTransient()
            );
        }
    }
}
