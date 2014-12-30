using System;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using KillrVideo.SampleData.Dtos;
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

        public SampleDataWorkerWindsorInstaller(string uniqueId)
        {
            if (uniqueId == null) throw new ArgumentNullException("uniqueId");
            _uniqueId = uniqueId;
        }

        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            // Configure the lease manager
            var leaseManagerConfig = new LeaseManagerConfig { LeaseName = "SampleDataJobs", UniqueId = _uniqueId };

            container.Register(
                // Assembly configuration for SampleData handlers/messages
                Component.For<NimbusAssemblyConfig>()
                         .Instance(NimbusAssemblyConfig.FromTypes(typeof (SampleDataWorkerWindsorInstaller), typeof (AddSampleUsers))),
                
                // Scheduler and related components
                Component.For<SampleDataJobScheduler>().LifestyleTransient(),
                Component.For<LeaseManager>().LifestyleTransient()
                         .DependsOn(Dependency.OnValue<LeaseManagerConfig>(leaseManagerConfig))
            );
        }
    }
}
