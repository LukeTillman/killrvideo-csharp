using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.Uploads.Messages.Events;
using KillrVideo.Uploads.Worker.InternalEvents;
using KillrVideo.Utils;
using log4net;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using Nimbus;

namespace KillrVideo.Uploads.Worker
{
    /// <summary>
    /// A job that listens for notifications from Azure Media Services about encoding job progress and logs the events in C*, as well
    /// as publishes events on the Bus about the status of the encoding job.
    /// </summary>
    public class EncodingListenerJob
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (EncodingListenerJob));
        private const string PoisionQueueName = UploadConfig.NotificationQueueName + "-errors";

        private readonly CloudQueueClient _cloudQueueClient;
        private readonly ISession _session;
        private readonly TaskCache<string, PreparedStatement> _statementCache;
        private readonly IBus _bus;

        private CloudQueue _queue;
        private CloudQueue _poisonQueue;
        private bool _initialized;

        private const int MessagesPerGet = 10;
        private const int MaxRetries = 6;

        public EncodingListenerJob(CloudQueueClient cloudQueueClient, ISession session, TaskCache<string, PreparedStatement> statementCache, IBus bus)
        {
            if (cloudQueueClient == null) throw new ArgumentNullException("cloudQueueClient");
            if (session == null) throw new ArgumentNullException("session");
            if (statementCache == null) throw new ArgumentNullException("statementCache");
            if (bus == null) throw new ArgumentNullException("bus");
            
            _cloudQueueClient = cloudQueueClient;
            _session = session;
            _statementCache = statementCache;
            _bus = bus;
            
            _initialized = false;
        }

        private async Task Initialize()
        {
            // Create the queues if they don't exist
            _queue = _cloudQueueClient.GetQueueReference(UploadConfig.NotificationQueueName);
            _poisonQueue = _cloudQueueClient.GetQueueReference(PoisionQueueName);
            await Task.WhenAll(_queue.CreateIfNotExistsAsync(), _poisonQueue.CreateIfNotExistsAsync()).ConfigureAwait(false);
            _initialized = true;
        }

        /// <summary>
        /// Starts the encoding listener job.
        /// </summary>
        public void Start()
        {
            
        }

        /// <summary>
        /// Stops the encoding listener job.
        /// </summary>
        public void Stop()
        {
            
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

        private async Task ExecuteImpl(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_initialized == false)
                await Initialize().ConfigureAwait(false);
            
            bool gotSomeMessages;
            do
            {
                // Always start by assuming we won't get any messages from the queue
                gotSomeMessages = false;

                // Get a batch of messages
                IEnumerable<CloudQueueMessage> messages = await _queue.GetMessagesAsync(MessagesPerGet, cancellationToken).ConfigureAwait(false);
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
                        await _queue.DeleteMessageAsync(message, cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    // Ignore any messages that aren't for JobStateChanges
                    if (jobEvent.IsJobStateChangeEvent() == false)
                    {
                        await _queue.DeleteMessageAsync(message, cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    // Otherwise, handle the event
                    bool handledSuccessfully = false;
                    try
                    {
                        await HandleEncodingJobEvent(jobEvent).ConfigureAwait(false);
                        handledSuccessfully = true;
                    }
                    catch (Exception e)
                    {
                        Logger.Error("Error while handling JobStateChanged message", e);
                    }

                    // If the message was handled successfully, just delete it
                    if (handledSuccessfully)
                    {
                        await _queue.DeleteMessageAsync(message, cancellationToken).ConfigureAwait(false);
                        continue;
                    }
                        
                    // If the message is over the number of retries, consider it a poison message, log it and delete it
                    if (jobEvent.RetryAttempts >= MaxRetries)
                    {
                        // Move the message to a poison queue (NOTE: because Add + Delete aren't "transactional" it is possible
                        // a poison message might get added more than once to the poison queue, but that's OK)
                        Logger.Fatal(string.Format("Giving up on message: {0}", message.AsString));
                        await _poisonQueue.AddMessageAsync(message, cancellationToken).ConfigureAwait(false);
                        await _queue.DeleteMessageAsync(message, cancellationToken).ConfigureAwait(false);
                        continue;
                    }
                    
                    // Increment the retry attempts and then modify the message in place so it will be processed again
                    int secondsUntilRetry = (2 ^ jobEvent.RetryAttempts)*10;
                    jobEvent.RetryAttempts++;
                    message.SetMessageContent(JsonConvert.SerializeObject(jobEvent));
                    await _queue.UpdateMessageAsync(message, TimeSpan.FromSeconds(secondsUntilRetry),
                                                    MessageUpdateFields.Content | MessageUpdateFields.Visibility, cancellationToken).ConfigureAwait(false);
                }

                // If we got some messages from the queue, keep processing until we don't get any
            } while (gotSomeMessages);

            // Exit method to allow a cooldown period (10s) between polling the queue whenever we run out of messages
        }

        private async Task HandleEncodingJobEvent(EncodingJobEvent notification)
        {
            string jobId = notification.GetJobId();

            // Lookup the uploaded video's Id by job Id
            PreparedStatement lookupPrepared =
                await _statementCache.NoContext.GetOrAddAsync("SELECT videoid FROM uploaded_video_jobs_by_jobid WHERE jobid = ?");
            RowSet lookupRows = await _session.ExecuteAsync(lookupPrepared.Bind(jobId)).ConfigureAwait(false);
            Row lookupRow = lookupRows.SingleOrDefault();
            if (lookupRow == null)
                throw new InvalidOperationException(string.Format("Could not find video for job id {0}", jobId));

            var videoId = lookupRow.GetValue<Guid>("videoid");

            // Log the event to C* (this should be idempotent in case of dupliacte tries since we're keyed by the job id, date, and etag in C*)
            PreparedStatement preparedStatement = await _statementCache.NoContext.GetOrAddAsync(
                "INSERT INTO encoding_job_notifications (videoid, status_date, etag, jobId, newstate, oldstate) VALUES (?, ?, ?, ?, ?, ?) USING TIMESTAMP ?");

            string newState = notification.GetNewState();
            string oldState = notification.GetOldState();
            DateTimeOffset statusDate = notification.TimeStamp;

            // INSERT INTO encoding_job_notifications ...
            await _session.ExecuteAsync(
                preparedStatement.Bind(videoId, statusDate, notification.ETag, jobId, newState, oldState, statusDate.ToMicrosecondsSinceEpoch())).ConfigureAwait(false);

            // See if the job has finished and if not, just bail
            if (notification.IsJobFinished() == false)
                return;

            // Publish the appropriate event based on whether the job was successful or not
            if (notification.WasSuccessful())
            {
                await _bus.Publish(new UploadedVideoProcessingSucceeded
                {
                    VideoId = videoId,
                    Timestamp = statusDate
                }).ConfigureAwait(false);
                return;
            }

            await _bus.Publish(new UploadedVideoProcessingFailed
            {
                VideoId = videoId,
                Timestamp = statusDate
            }).ConfigureAwait(false);
        }
    }
}
