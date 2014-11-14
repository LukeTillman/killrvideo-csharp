using System.Configuration;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using KillrVideo.Uploads.Messages.Commands;
using KillrVideo.Uploads.Worker.Jobs;
using KillrVideo.Utils.Nimbus;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.MediaServices.Client;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;

namespace KillrVideo.Uploads.Worker
{
    /// <summary>
    /// Castle Windsor installer for installing all the components needed by the Uploads worker.
    /// </summary>
    public class UploadsWindsorInstaller : IWindsorInstaller
    {
        private const string MediaServicesNameAppSettingsKey = "AzureMediaServicesAccountName";
        private const string MediaServicesKeyAppSettingsKey = "AzureMediaServicesAccountKey";
        private const string StorageConnectionStringAppSettingsKey = "AzureStorageConnectionString";

        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(
                Component.For<NimbusAssemblyConfig>()
                         .Instance(NimbusAssemblyConfig.FromTypes(typeof(UploadsWindsorInstaller), typeof(AddUploadedVideo))));

            // Register the Uploads components as singletons so their state can be reused (prepared statements)
            container.Register(
                Classes.FromAssemblyContaining<UploadedVideosWriteModel>().Pick()
                       .WithServiceFirstInterface().LifestyleSingleton());

            // Register all IUploadWorkerJobs in this assembly
            container.Register(Classes.FromThisAssembly().BasedOn<IUploadWorkerJob>().WithServiceBase().LifestyleTransient());

            // Register Azure components
            RegisterAzureComponents(container);
        }

        private static void RegisterAzureComponents(IWindsorContainer container)
        {
            // Get Azure configurations
            string mediaServicesAccountName = GetRequiredSetting(MediaServicesNameAppSettingsKey);
            string mediaServicesAccountKey = GetRequiredSetting(MediaServicesKeyAppSettingsKey);
            string storageConnectionString = GetRequiredSetting(StorageConnectionStringAppSettingsKey);

            var mediaCredentials = new MediaServicesCredentials(mediaServicesAccountName, mediaServicesAccountKey);
            container.Register(
                // Recommended be shared by all CloudMediaContext objects so register as singleton
                Component.For<MediaServicesCredentials>().Instance(mediaCredentials),

                // Not thread-safe, so register as transient
                Component.For<CloudMediaContext>().LifestyleTransient()
            );

            // Setup queue for notifications about video encoding jobs
            var storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            container.Register(
                // Register the queue client and get a new one each time (transient) just to be safe
                Component.For<CloudQueueClient>().UsingFactoryMethod(storageAccount.CreateCloudQueueClient).LifestyleTransient()
            );
        }

        /// <summary>
        /// Gets a required setting from CloudConfigurationManager and throws a ConfigurationErrorsException if setting is null/empty.
        /// </summary>
        private static string GetRequiredSetting(string key)
        {
            var value = CloudConfigurationManager.GetSetting(key);
            if (string.IsNullOrEmpty(value))
                throw new ConfigurationErrorsException(string.Format("No value for required setting {0} in cloud configuration", key));

            return value;
        }
    }
}
