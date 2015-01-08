using Castle.MicroKernel;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using KillrVideo.SampleData.Worker.Components;
using KillrVideo.SampleData.Worker.Scheduler;
using KillrVideo.Utils.Configuration;

namespace KillrVideo.SampleData.Worker
{
    /// <summary>
    /// Windsor installer for installing all the components needed by the SampleData worker.
    /// </summary>
    public class SampleDataWorkerWindsorInstaller : IWindsorInstaller
    {
        private const string YouTubeApiKey = "YouTubeApiKey";

        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            // Register components
            container.Register(
                // Scheduler and related components
                Component.For<SampleDataJobScheduler>().LifestyleTransient(),
                Component.For<LeaseManager>().LifestyleTransient(),
                Component.For<LeaseManagerConfig>().UsingFactoryMethod(CreateLeaseManagerConfig).LifestyleSingleton(),

                // Scheduler jobs
                Classes.FromThisAssembly().BasedOn<SampleDataJob>().WithServiceBase().LifestyleTransient(),

                // Other components
                Classes.FromThisAssembly().InSameNamespaceAs<GetSampleData>(includeSubnamespaces: true)
                       .WithServiceFirstInterface().WithServiceSelf().LifestyleTransient(),

                // YouTube API client
                Component.For<YouTubeService>().UsingFactoryMethod(CreateYouTubeService).LifestyleSingleton()
            );
        }

        private static LeaseManagerConfig CreateLeaseManagerConfig(IKernel kernel)
        {
            // Configure the lease manager
            var configRetriever = kernel.Resolve<IGetEnvironmentConfiguration>();
            var leaseManagerConfig = new LeaseManagerConfig { LeaseName = "SampleDataJobs", UniqueId = configRetriever.UniqueInstanceId };
            kernel.ReleaseComponent(configRetriever);

            return leaseManagerConfig;
        }

        private static YouTubeService CreateYouTubeService(IKernel kernel)
        {
            var configRetriever = kernel.Resolve<IGetEnvironmentConfiguration>();
            string apiKey = configRetriever.GetSetting(YouTubeApiKey);
            kernel.ReleaseComponent(configRetriever);

            // Create client for YouTube API
            var youTubeInit = new BaseClientService.Initializer
            {
                ApiKey = apiKey,
                ApplicationName = "KillrVideo.SampleData.Worker"
            };
            return new YouTubeService(youTubeInit);
        }
    }
}
