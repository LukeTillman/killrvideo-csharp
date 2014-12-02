using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KillrVideo.Uploads.Dtos;
using log4net;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;

namespace KillrVideo.Uploads.Worker.EncodingJobMonitor
{
    /// <summary>
    /// A job that listens for notifications from Azure Media Services about encoding job progress and dispatches
    /// the events to a monitor to handle them.
    /// </summary>
    public class EncodingListenerJob
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (EncodingListenerJob));
        private const string PoisionQueueName = UploadConfig.NotificationQueueName + "-errors";

        private readonly CloudQueueClient _cloudQueueClient;
        private readonly IMonitorEncodingJobs _monitor;
        
        private CloudQueue _queue;
        private CloudQueue _poisonQueue;
        private bool _initialized;

        private const int MessagesPerGet = 10;
        private const int MaxRetries = 6;

        public EncodingListenerJob(CloudQueueClient cloudQueueClient, IMonitorEncodingJobs monitor)
        {
            if (cloudQueueClient == null) throw new ArgumentNullException("cloudQueueClient");
            if (monitor == null) throw new ArgumentNullException("monitor");

            _cloudQueueClient = cloudQueueClient;
            _monitor = monitor;

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

        /// <summary>
        /// Executes the job.
        /// </summary>
        public async Task Execute(CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await ExecuteImpl(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    Logger.Error("Error while processing job", e);
                }

                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);
            }
        }

        protected async Task ExecuteImpl(CancellationToken cancellationToken)
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
                        await _monitor.HandleEncodingJobEvent(jobEvent);
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
    }
}
