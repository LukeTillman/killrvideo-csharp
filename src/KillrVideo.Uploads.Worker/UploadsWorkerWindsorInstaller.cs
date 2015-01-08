using System.Linq;
using Castle.MicroKernel;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using KillrVideo.Utils.Configuration;
using Microsoft.WindowsAzure.MediaServices.Client;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;

namespace KillrVideo.Uploads.Worker
{
    /// <summary>
    /// Castle Windsor installer for installing all the components needed by the Uploads worker.
    /// </summary>
    public class UploadsWorkerWindsorInstaller : IWindsorInstaller
    {
        private const string MediaServicesNameAppSettingsKey = "AzureMediaServicesAccountName";
        private const string MediaServicesKeyAppSettingsKey = "AzureMediaServicesAccountKey";
        private const string StorageConnectionStringAppSettingsKey = "AzureStorageConnectionString";

        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(
                // Azure Media Services components
                Component.For<MediaServicesCredentials>().UsingFactoryMethod(CreateCredentials).LifestyleSingleton(),
                Component.For<CloudMediaContext>().LifestyleTransient(),
                Component.For<INotificationEndPoint>().UsingFactoryMethod(CreateNotificationEndPoint).LifestyleSingleton(),

                // Azure Storage components
                Component.For<CloudStorageAccount>().UsingFactoryMethod(CreateCloudStorageAccount).LifestyleTransient(),
                Component.For<CloudQueueClient>().UsingFactoryMethod(CreateCloudQueueClient).LifestyleTransient(),
                
                // Job for listening to Azure Media Services notifications
                Component.For<EncodingListenerJob>().LifestyleTransient()
            );
        }

        private static MediaServicesCredentials CreateCredentials(IKernel kernel)
        {
            // Get Azure configurations
            var configRetriever = kernel.Resolve<IGetEnvironmentConfiguration>();
            string mediaServicesAccountName = configRetriever.GetSetting(MediaServicesNameAppSettingsKey);
            string mediaServicesAccountKey = configRetriever.GetSetting(MediaServicesKeyAppSettingsKey);
            kernel.ReleaseComponent(configRetriever);

            // Return the credentials
            return new MediaServicesCredentials(mediaServicesAccountName, mediaServicesAccountKey);
        }

        private static CloudStorageAccount CreateCloudStorageAccount(IKernel kernel)
        {
            // Get Azure configurations
            var configRetriever = kernel.Resolve<IGetEnvironmentConfiguration>();
            string storageConnectionString = configRetriever.GetSetting(StorageConnectionStringAppSettingsKey);
            kernel.ReleaseComponent(configRetriever);

            return CloudStorageAccount.Parse(storageConnectionString);
        }

        private static CloudQueueClient CreateCloudQueueClient(IKernel kernel)
        {
            var storageAccount = kernel.Resolve<CloudStorageAccount>();
            CloudQueueClient client = storageAccount.CreateCloudQueueClient();
            kernel.ReleaseComponent(storageAccount);

            return client;
        }

        private static INotificationEndPoint CreateNotificationEndPoint(IKernel kernel)
        {
            var cloudMediaContext = kernel.Resolve<CloudMediaContext>();
            var cloudQueueClient = kernel.Resolve<CloudQueueClient>();

            try
            {
                // ReSharper disable once ReplaceWithSingleCallToFirstOrDefault
                INotificationEndPoint endpoint = cloudMediaContext.NotificationEndPoints
                                                                   .Where(ep => ep.Name == UploadConfig.NotificationQueueName)
                                                                   .FirstOrDefault();
                if (endpoint != null)
                    return endpoint;

                // Make sure the queue exists in Azure Storage
                var notificationQueue = cloudQueueClient.GetQueueReference(UploadConfig.NotificationQueueName);
                notificationQueue.CreateIfNotExists();

                // Create the endpoint
                return cloudMediaContext.NotificationEndPoints.Create(UploadConfig.NotificationQueueName, NotificationEndPointType.AzureQueue,
                                                                       UploadConfig.NotificationQueueName);
            }
            finally
            {
                kernel.ReleaseComponent(cloudMediaContext);
                kernel.ReleaseComponent(cloudQueueClient);
            }
        }
    }
}
