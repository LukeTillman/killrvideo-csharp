using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KillrVideo.Data;
using KillrVideo.Data.Upload;
using KillrVideo.Data.Upload.Dtos;
using KillrVideo.Data.Videos;
using KillrVideo.Data.Videos.Dtos;
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
        private readonly CloudQueue _queue;
        private readonly CloudQueue _poisonQueue;

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
            _cloudMediaContext = cloudMediaContext;
            _uploadWriteModel = uploadWriteModel;
            _uploadReadModel = uploadReadModel;
            _videoWriteModel = videoWriteModel;

            _queue = cloudQueueClient.GetQueueReference(UploadConfig.NotificationQueueName);

            // Get the poison message queue and create it if it doesn't already exist
            _poisonQueue = cloudQueueClient.GetQueueReference(PoisionQueueName);
            _poisonQueue.CreateIfNotExists();
        }

        protected override async Task ExecuteImpl(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

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
                        jobEvent = JsonConvert.DeserializeObject<EncodingJobEvent>(message.AsString);
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
                
                // Publish the asset by creating an on demand locator for it if one isn't already present (check for one already present
                // in case of duplicate/partial failures since we're limited on how many an asset can have)
                ILocator locator = asset.Locators.Where(l => l.Type == LocatorType.OnDemandOrigin).FirstOrDefault();
                if (locator == null)
                {
                    const AccessPermissions readPermissions = AccessPermissions.Read | AccessPermissions.List;
                    locator = await _cloudMediaContext.Locators.CreateAsync(LocatorType.OnDemandOrigin, asset, readPermissions,
                                                                            PublishedVideosGoodFor);
                }

                // Get the URL for streaming from the locator
                string location = locator.GetMpegDashUri().AbsoluteUri;

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

                // Get the URL for the first thumbnail file in the asset
                List<IAssetFile> jpgFiles = thumbnailAsset.AssetFiles.ToList();
                var thumbnailLocation = new UriBuilder(thumbnailLocator.Path);
                thumbnailLocation.Path += "/" + jpgFiles.First(f => f.Name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)).Name;
                
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
                    Location = location,
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
