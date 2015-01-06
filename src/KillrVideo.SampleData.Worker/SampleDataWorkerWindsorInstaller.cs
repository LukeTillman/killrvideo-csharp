using System;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using KillrVideo.SampleData.Dtos;
using KillrVideo.SampleData.Worker.Components;
using KillrVideo.SampleData.Worker.Scheduler;
using KillrVideo.Utils.Nimbus;

namespace KillrVideo.SampleData.Worker
{
    /// <summary>
    /// Windsor installer for installing all the components needed by the SampleData worker.
    /// </summary>
    public class SampleDataWorkerWindsorInstaller : IWindsorInstaller
    {
        private readonly string _uniqueId;
        private readonly string _youTubeApiKey;

        public SampleDataWorkerWindsorInstaller(string uniqueId, string youTubeApiKey)
        {
            if (uniqueId == null) throw new ArgumentNullException("uniqueId");
            if (youTubeApiKey == null) throw new ArgumentNullException("youTubeApiKey");
            _uniqueId = uniqueId;
            _youTubeApiKey = youTubeApiKey;
        }

        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            // Configure the lease manager
            var leaseManagerConfig = new LeaseManagerConfig { LeaseName = "SampleDataJobs", UniqueId = _uniqueId };

            // Register components
            container.Register(
                // Assembly configuration for SampleData handlers/messages
                Component.For<NimbusAssemblyConfig>()
                         .Instance(NimbusAssemblyConfig.FromTypes(typeof (SampleDataWorkerWindsorInstaller), typeof (AddSampleUsers))),

                // Scheduler and related components
                Component.For<SampleDataJobScheduler>().LifestyleTransient(),
                Component.For<LeaseManager>().LifestyleTransient()
                         .DependsOn(Dependency.OnValue<LeaseManagerConfig>(leaseManagerConfig)),

                // Scheduler jobs
                Classes.FromThisAssembly().BasedOn<SampleDataJob>().WithServiceBase().LifestyleTransient(),

                // Other components
                Classes.FromThisAssembly().InSameNamespaceAs<GetSampleData>(includeSubnamespaces: true)
                       .WithServiceFirstInterface().WithServiceSelf().LifestyleTransient()
                );

            // Create client for YouTube API
            var youTubeInit = new BaseClientService.Initializer
            {
                ApiKey = _youTubeApiKey,
                ApplicationName = "KillrVideo.SampleData.Worker"
            };
            var youTubeClient = new YouTubeService(youTubeInit);

            container.Register(Component.For<YouTubeService>().Instance(youTubeClient));

            // Also install the sample data service since it's used by the scheduler jobs
            container.Install(new SampleDataServiceWindsorInstaller());
        }
    }
}
