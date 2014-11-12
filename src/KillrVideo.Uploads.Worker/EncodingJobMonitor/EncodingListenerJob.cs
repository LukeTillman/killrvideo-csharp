using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KillrVideo.Uploads;
using KillrVideo.Uploads.Dtos;
using KillrVideo.UploadWorker.Jobs;
using log4net;
using Microsoft.WindowsAzure.MediaServices.Client;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;

namespace KillrVideo.UploadWorker.EncodingJobMonitor
{
    // ReSharper disable ReplaceWithSingleCallToSingle
    // ReSharper disable ReplaceWithSingleCallToFirstOrDefault
    // ReSharper disable ReplaceWithSingleCallToSingleOrDefault

    /// <summary>
    /// A job that listens for notifications from Azure Media Services about encoding job progress and 
    /// </summary>
    public class EncodingListenerJob : UploadWorkerJobBase
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (EncodingListenerJob));
        private static readonly TimeSpan PublishedVideosGoodFor = TimeSpan.FromDays(10000);
        private const string PoisionQueueName = UploadConfig.NotificationQueueName + "-errors";

        private readonly CloudMediaContext _cloudMediaContext;
        private readonly IUploadedVideosWriteModel _uploadWriteModel;
        private readonly IUploadedVideosReadModel _uploadReadModel;
        private readonly IVideoWriteModel _videoWriteModel;
        private readonly CloudQueueClient _cloudQueueClient;
        private readonly Random _random;

        private CloudQueue _queue;
        private CloudQueue _poisonQueue;
        private bool _initialized;

        private const int MessagesPerGet = 10;
        private const int MaxRetries = 6;

        public EncodingListenerJob(CloudQueueClient cloudQueueClient, CloudMediaContext cloudMediaContext,
                                   IUploadedVideosWriteModel uploadWriteModel, IUploadedVideosReadModel uploadReadModel,
                                   IVideoWriteModel videoWriteModel)
        {
            if (cloudQueueClient == null) throw new ArgumentNullException("cloudQueueClient");
            if (cloudMediaContext == null) throw new ArgumentNullException("cloudMediaContext");
            if (uploadWriteModel == null) throw new ArgumentNullException("uploadWriteModel");
            if (uploadReadModel == null) throw new ArgumentNullException("uploadReadModel");
            if (videoWriteModel == null) throw new ArgumentNullException("videoWriteModel");
            _cloudQueueClient = cloudQueueClient;
            _cloudMediaContext = cloudMediaContext;
            _uploadWriteModel = uploadWriteModel;
            _uploadReadModel = uploadReadModel;
            _videoWriteModel = videoWriteModel;

            _random = new Random();
            _initialized = false;
        }

        private async Task Initialize()
        {
            // Create the queues if they don't exist
            _queue = _cloudQueueClient.GetQueueReference(UploadConfig.NotificationQueueName);
            _poisonQueue = _cloudQueueClient.GetQueueReference(PoisionQueueName);
            await Task.WhenAll(_queue.CreateIfNotExistsAsync(), _poisonQueue.CreateIfNotExistsAsync());
            _initialized = true;
        }

        protected override async Task ExecuteImpl(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_initialized == false)
                await Initialize();
            
            bool gotSomeMessages;
            do
            {
                // Always start by assuming we won't get any messages from the queue
                gotSomeMessages = false;

                // Get a batch of messages
                IEnumerable<CloudQueueMessage> messages = await _queue.GetMessagesAsync(MessagesPerGet, cancellationToken);
                foreach (CloudQueueMessage message in messages)
                {
                    // Check for cancellation before processing a message
                    cancellationToken.ThrowIfCancellationRequested();

                    // We obviously got some messages since we're processing one
                    gotSomeMessages = true;

                    // Try to deserialize the message to an EncodingJobEvent
                    EncodingJobEvent jobEvent = null;
                    try
                    {
                        var settings = new JsonSerializerSettings {DateTimeZoneHandling = DateTimeZoneHandling.Utc};
                        jobEvent = JsonConvert.DeserializeObject<EncodingJobEvent>(message.AsString, settings);
                    }
                    catch (Exception e)
                    {
                        Logger.Warn("Exception while deserializing event, message will be deleted.", e);
                    }

                    // If there was a problem with deserialization, just assume a poison message and delete it
                    if (jobEvent == null)
                    {
                        await _queue.DeleteMessageAsync(message, cancellationToken);
                        continue;
                    }

                    // Ignore any messages that aren't for JobStateChanges
                    if (jobEvent.IsJobStateChangeEvent() == false)
                    {
                        await _queue.DeleteMessageAsync(message, cancellationToken);
                        continue;
                    }

                    // Otherwise, handle the event
                    bool handledSuccessfully = false;
                    try
                    {
                        await HandleJobStateChange(jobEvent);
                        handledSuccessfully = true;
                    }
                    catch (Exception e)
                    {
                        Logger.Error("Error while handling JobStateChanged message", e);
                    }

                    // If the message was handled successfully, just delete it
                    if (handledSuccessfully)
                    {
                        await _queue.DeleteMessageAsync(message, cancellationToken);
                        continue;
                    }
                        
                    // If the message is over the number of retries, consider it a poison message, log it and delete it
                    if (jobEvent.RetryAttempts >= MaxRetries)
                    {
                        // Move the message to a poison queue (NOTE: because Add + Delete aren't "transactional" it is possible
                        // a poison message might get added more than once to the poison queue, but that's OK)
                        Logger.Fatal(string.Format("Giving up on message: {0}", message.AsString));
                        await _poisonQueue.AddMessageAsync(message, cancellationToken);
                        await _queue.DeleteMessageAsync(message, cancellationToken);
                        continue;
                    }
                    
                    // Increment the retry attempts and then modify the message in place so it will be processed again
                    int secondsUntilRetry = (2 ^ jobEvent.RetryAttempts)*10;
                    jobEvent.RetryAttempts++;
                    message.SetMessageContent(JsonConvert.SerializeObject(jobEvent));
                    await _queue.UpdateMessageAsync(message, TimeSpan.FromSeconds(secondsUntilRetry),
                                                    MessageUpdateFields.Content | MessageUpdateFields.Visibility, cancellationToken);
                }

                // If we got some messages from the queue, keep processing until we don't get any
            } while (gotSomeMessages);

            // Exit method to allow a cooldown period (10s) between polling the queue whenever we run out of messages
        }

        private async Task HandleJobStateChange(EncodingJobEvent jobEvent)
        {
            // Log the event to C* (this should be idempotent in case of dupliacte tries since we're keyed by the job id, date, and etag in C*)
            string jobId = jobEvent.GetJobId();
            await _uploadWriteModel.AddEncodingJobNotification(new AddEncodingJobNotification
            {
                JobId = jobId,
                StatusDate = jobEvent.TimeStamp,
                ETag = jobEvent.ETag,
                NewState = jobEvent.GetNewState(),
                OldState = jobEvent.GetOldState()
            });

            // If the job is completed successfully, add the video
            if (jobEvent.WasSuccessful())
            {
                // Find the job in Azure Media Services and throw if not found
                IJob job = _cloudMediaContext.Jobs.Where(j => j.Id == jobId).SingleOrDefault();
                if (job == null)
                    throw new InvalidOperationException(string.Format("Could not find job {0}", jobId));

                List<IAsset> outputAssets = job.OutputMediaAssets.ToList();

                // Find the encoded video asset
                IAsset asset = outputAssets.SingleOrDefault(a => a.Name.StartsWith(UploadConfig.EncodedVideoAssetNamePrefix));
                if (asset == null)
                    throw new InvalidOperationException(string.Format("Could not find video output asset for job {0}", jobId));
                
                // Publish the asset for progressive downloading (HTML5) by creating an SAS locator for it and adding the file name to the path
                ILocator locator = asset.Locators.Where(l => l.Type == LocatorType.Sas).FirstOrDefault();
                if (locator == null)
                {
                    const AccessPermissions readPermissions = AccessPermissions.Read | AccessPermissions.List;
                    locator = await _cloudMediaContext.Locators.CreateAsync(LocatorType.Sas, asset, readPermissions,
                                                                            PublishedVideosGoodFor);
                }

                // Get the URL for streaming from the locator (embed file name for the mp4 in locator before query string)
                IAssetFile mp4File = asset.AssetFiles.ToList().Single(f => f.Name.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase));
                var location = new UriBuilder(locator.Path);
                location.Path += "/" + mp4File.Name;

                // Find the thumbnail asset
                IAsset thumbnailAsset = outputAssets.SingleOrDefault(a => a.Name.StartsWith(UploadConfig.ThumbnailAssetNamePrefix));
                if (thumbnailAsset == null)
                    throw new InvalidOperationException(string.Format("Could not find thumbnail output asset for job {0}", jobId));

                // Publish the thumbnail asset by creating a locator for it (again, check if already present)
                ILocator thumbnailLocator = thumbnailAsset.Locators.Where(l => l.Type == LocatorType.Sas).FirstOrDefault();
                if (thumbnailLocator == null)
                {
                    thumbnailLocator = await _cloudMediaContext.Locators.CreateAsync(LocatorType.Sas, thumbnailAsset, AccessPermissions.Read,
                                                                                     PublishedVideosGoodFor);
                }

                // Get the URL for a random thumbnail file in the asset
                List<IAssetFile> jpgFiles =
                    thumbnailAsset.AssetFiles.ToList().Where(f => f.Name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)).ToList();
                var thumbnailLocation = new UriBuilder(thumbnailLocator.Path);
                int randomThumbnailIndex = _random.Next(jpgFiles.Count);
                thumbnailLocation.Path += "/" + jpgFiles[randomThumbnailIndex].Name;
                
                UploadedVideo uploadedVideo = await _uploadReadModel.GetByJobId(jobId);
                if (uploadedVideo == null)
                    throw new InvalidOperationException(string.Format("Could not find uploaded video data for job {0}", jobId));

                await _videoWriteModel.AddVideo(new AddVideo
                {
                    VideoId = uploadedVideo.VideoId,
                    UserId = uploadedVideo.UserId,
                    Name = uploadedVideo.Name,
                    Description = uploadedVideo.Description,
                    Tags = uploadedVideo.Tags,
                    Location = location.Uri.AbsoluteUri,
                    LocationType = VideoLocationType.Upload,
                    PreviewImageLocation = thumbnailLocation.Uri.AbsoluteUri
                });
            }
        }
    }

    // ReSharper enable ReplaceWithSingleCallToSingle
    // ReSharper enable ReplaceWithSingleCallToFirstOrDefault
    // ReSharper enable ReplaceWithSingleCallToSingleOrDefault
}
