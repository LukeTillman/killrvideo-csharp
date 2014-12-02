using System;
using System.Linq;
using Microsoft.WindowsAzure.MediaServices.Client;
using Microsoft.WindowsAzure.Storage.Queue;

namespace KillrVideo.Uploads
{
    /// <summary>
    /// Factory for creating/getting the Azure Media Service endpoint for upload job notifications.
    /// </summary>
    public class NotificationEndPointFactory
    {
        private readonly CloudMediaContext _cloudMediaContext;
        private readonly CloudQueueClient _cloudQueueClient;

        public NotificationEndPointFactory(CloudMediaContext cloudMediaContext, CloudQueueClient cloudQueueClient)
        {
            if (cloudMediaContext == null) throw new ArgumentNullException("cloudMediaContext");
            if (cloudQueueClient == null) throw new ArgumentNullException("cloudQueueClient");
            _cloudMediaContext = cloudMediaContext;
            _cloudQueueClient = cloudQueueClient;
        }

        /// <summary>
        /// Gets the upload notification endpoint in Azure Media Services.
        /// </summary>
        public INotificationEndPoint GetNotificationEndPoint()
        {
            // ReSharper disable once ReplaceWithSingleCallToFirstOrDefault
            INotificationEndPoint endpoint = _cloudMediaContext.NotificationEndPoints
                                                               .Where(ep => ep.Name == UploadConfig.NotificationQueueName)
                                                               .FirstOrDefault();
            if (endpoint != null)
                return endpoint;

            // Make sure the queue exists in Azure Storage
            var notificationQueue = _cloudQueueClient.GetQueueReference(UploadConfig.NotificationQueueName);
            notificationQueue.CreateIfNotExists();

            // Create the endpoint
            return _cloudMediaContext.NotificationEndPoints.Create(UploadConfig.NotificationQueueName, NotificationEndPointType.AzureQueue,
                                                                   UploadConfig.NotificationQueueName);
        }
    }
}
