using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
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
    /// <summary>
    /// A job that listens for notifications from Azure Media Services about encoding job progress and 
    /// </summary>
    public class EncodingListenerJob : UploadWorkerJobBase
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (EncodingListenerJob));

        private readonly CloudMediaContext _cloudMediaContext;
        private readonly IUploadedVideosWriteModel _uploadWriteModel;
        private readonly IUploadedVideosReadModel _uploadReadModel;
        private readonly IVideoWriteModel _videoWriteModel;
        private readonly CloudQueue _queue;

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

            _queue = cloudQueueClient.GetQueueReference(UploadConfigConstants.NotificationQueueName);
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
                        // TODO: Poison message queue?
                        Logger.Fatal(string.Format("Giving up on message: {0}", message.AsString));
                        await _queue.DeleteMessageAsync(message, cancellationToken);
                        continue;
                    }

                    // Increment the retry attempts and then modify the message in place so it will be processed again (60 seconds from now)
                    // TODO: Exponential backoff for retries?
                    jobEvent.RetryAttempts++;
                    message.SetMessageContent(JsonConvert.SerializeObject(jobEvent));
                    await _queue.UpdateMessageAsync(message, TimeSpan.FromSeconds(60), MessageUpdateFields.Content | MessageUpdateFields.Visibility,
                                                    cancellationToken);
                }

                // If we got some messages from the queue, keep processing until we don't get any
            } while (gotSomeMessages);

            // Exit method to allow a cooldown period (10s) between polling the queue whenever we run out of messages
        }

        private async Task HandleJobStateChange(EncodingJobEvent jobEvent)
        {
            // Log the event to C*
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
                // Find the job in Azure Media Services and thus the output asset that was encoded
                // ReSharper disable once ReplaceWithSingleCallToSingle
                IJob job = _cloudMediaContext.Jobs.Where(j => j.Id == jobId).Single();
                IAsset asset = job.OutputMediaAssets.Single();
                
                // Publish the asset by creating an on demand locator for it
                const AccessPermissions readPermissions = AccessPermissions.Read | AccessPermissions.List;
                ILocator locator = await _cloudMediaContext.Locators.CreateAsync(LocatorType.OnDemandOrigin, asset, readPermissions,
                                                                                 TimeSpan.FromDays(10000));

                // Get the URL for streaming
                string location = locator.GetMpegDashUri().AbsoluteUri;

                UploadedVideo uploadedVideo = await _uploadReadModel.GetByJobId(jobId);
                if (uploadedVideo == null)
                    throw new InvalidOperationException(string.Format("Could not find uploaded video for job {0}", jobId));

                await _videoWriteModel.AddVideo(new AddVideo
                {
                    VideoId = uploadedVideo.VideoId,
                    UserId = uploadedVideo.UserId,
                    Name = uploadedVideo.Name,
                    Description = uploadedVideo.Description,
                    Tags = uploadedVideo.Tags,
                    Location = location, // TODO
                    LocationType = VideoLocationType.Upload,
                    PreviewImageLocation = string.Empty // TODO
                });
            }
        }
    }
}
