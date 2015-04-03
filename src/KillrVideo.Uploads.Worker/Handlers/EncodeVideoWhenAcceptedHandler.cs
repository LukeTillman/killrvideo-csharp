using System;
using System.Linq;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.Uploads.Messages.Events;
using KillrVideo.Utils;
using KillrVideo.VideoCatalog.Messages.Events;
using Microsoft.WindowsAzure.MediaServices.Client;
using Nimbus;
using Nimbus.Handlers;

namespace KillrVideo.Uploads.Worker.Handlers
{
    /// <summary>
    /// Handler for kicking off an encoding job once an uploaded video has been accepted.
    /// </summary>
    public class EncodeVideoWhenAcceptedHandler : IHandleCompetingEvent<UploadedVideoAccepted>
    {
        private readonly ISession _session;
        private readonly TaskCache<string, PreparedStatement> _statementCache;
        private readonly IBus _bus;
        private readonly CloudMediaContext _cloudMediaContext;
        private readonly INotificationEndPoint _notificationEndPoint;

        public EncodeVideoWhenAcceptedHandler(ISession session, TaskCache<string, PreparedStatement> statementCache, IBus bus, CloudMediaContext cloudMediaContext,
                                              INotificationEndPoint notificationEndPoint)
        {
            if (session == null) throw new ArgumentNullException("session");
            if (statementCache == null) throw new ArgumentNullException("statementCache");
            if (bus == null) throw new ArgumentNullException("bus");
            if (cloudMediaContext == null) throw new ArgumentNullException("cloudMediaContext");
            if (notificationEndPoint == null) throw new ArgumentNullException("notificationEndPoint");
            _session = session;
            _statementCache = statementCache;
            _bus = bus;
            _cloudMediaContext = cloudMediaContext;
            _notificationEndPoint = notificationEndPoint;
        }

        // ReSharper disable ReplaceWithSingleCallToFirstOrDefault

        public async Task Handle(UploadedVideoAccepted uploadAccepted)
        {
            // Find the uploaded file's information
            PreparedStatement prepared = await _statementCache.NoContext.GetOrAddAsync("SELECT * FROM uploaded_video_destinations WHERE upload_url = ?");
            RowSet rows = await _session.ExecuteAsync(prepared.Bind(uploadAccepted.UploadUrl)).ConfigureAwait(false);
            Row row = rows.SingleOrDefault();
            if (row == null)
                throw new InvalidOperationException(string.Format("Could not find uploaded video with URL {0}", uploadAccepted.UploadUrl));

            var assetId = row.GetValue<string>("assetid");
            var filename = row.GetValue<string>("filename");

            // Find the asset to be encoded
            IAsset asset = _cloudMediaContext.Assets.Where(a => a.Id == assetId).FirstOrDefault();
            if (asset == null)
                throw new InvalidOperationException(string.Format("Could not find asset {0}.", assetId));

            // Try to avoid creating a second encoding job for a video if we get this message more than once
            PreparedStatement prepGetJob = await _statementCache.NoContext.GetOrAddAsync("SELECT * FROM uploaded_video_jobs WHERE videoid = ?");
            RowSet getJobRows = await _session.ExecuteAsync(prepGetJob.Bind(uploadAccepted.VideoId));
            Row getJobRow = getJobRows.SingleOrDefault();

            string jobId = getJobRow == null ? null : getJobRow.GetValue<string>("jobid");
            if (jobId == null)
            {
                // Create a job with a single task to encode the video
                string outputAssetName = string.Format("{0}{1} ({2})", UploadConfig.EncodedVideoAssetNamePrefix, filename, uploadAccepted.VideoId);
                IJob job = _cloudMediaContext.Jobs.CreateWithSingleTask(MediaProcessorNames.WindowsAzureMediaEncoder,
                                                                        UploadConfig.VideoGenerationXml, asset,
                                                                        outputAssetName, AssetCreationOptions.None);
                
                job.Name = string.Format("Encoding {0} ({1})", filename, uploadAccepted.VideoId);
                ITask encodingTask = job.Tasks.Single();
                encodingTask.Name = string.Format("Re-encode Video - {0}", filename);

                // Get a reference to the asset for the re-encoded video
                IAsset encodedAsset = encodingTask.OutputAssets.Single();

                // Add a task to create thumbnails to the encoding job
                string taskName = string.Format("Create Thumbnails - {0}", filename);
                IMediaProcessor processor = _cloudMediaContext.MediaProcessors.GetLatestMediaProcessorByName(MediaProcessorNames.WindowsAzureMediaEncoder);
                ITask task = job.Tasks.AddNew(taskName, processor, UploadConfig.ThumbnailGenerationXml, TaskOptions.ProtectedConfiguration);

                // The task should use the encoded file from the first task as input and output thumbnails in a new asset
                task.InputAssets.Add(encodedAsset);
                task.OutputAssets.AddNew(string.Format("{0}{1} ({2})", UploadConfig.ThumbnailAssetNamePrefix, filename, uploadAccepted.VideoId),
                                         AssetCreationOptions.None);

                // Get status upades on the job's progress on Azure queue, then start the job
                job.JobNotificationSubscriptions.AddNew(NotificationJobState.All, _notificationEndPoint);
                await job.SubmitAsync().ConfigureAwait(false);

                jobId = job.Id;

                // Insert the data into the uploaded_video_jobs table using LWT to ensure only one gets input for a given video id
                PreparedStatement prepSaveJob = await _statementCache.NoContext.GetOrAddAsync(
                    "INSERT INTO uploaded_video_jobs (videoid, upload_url, jobid) VALUES (?, ?, ?) IF NOT EXISTS");
                RowSet saveJobRows =
                    await _session.ExecuteAsync(prepSaveJob.Bind(uploadAccepted.VideoId, uploadAccepted.UploadUrl, jobId)).ConfigureAwait(false);
                Row saveJobRow = saveJobRows.Single();

                // If the LWT failed, use the jobid that was already present
                var applied = saveJobRow.GetValue<bool>("[applied]");
                if (applied == false)
                    jobId = saveJobRow.GetValue<string>("jobid");
            }

            DateTimeOffset timestamp = DateTimeOffset.UtcNow;

            // Add the job information that exists to the other lookup table (by job id)
            PreparedStatement prepSaveByJobId = await _statementCache.NoContext.GetOrAddAsync(
                "INSERT INTO uploaded_video_jobs_by_jobid (jobid, videoid, upload_url) VALUES (?, ?, ?) USING TIMESTAMP ?");
            BoundStatement boundSaveByJobId = prepSaveByJobId.Bind(jobId, uploadAccepted.VideoId, uploadAccepted.UploadUrl,
                                                                   timestamp.ToMicrosecondsSinceEpoch());
            await _session.ExecuteAsync(boundSaveByJobId).ConfigureAwait(false);

            // Tell the world about the job
            await _bus.Publish(new UploadedVideoProcessingStarted
            {
                VideoId = uploadAccepted.VideoId,
                Timestamp = timestamp
            }).ConfigureAwait(false);
        }

        // ReSharper restore ReplaceWithSingleCallToFirstOrDefault
    }
}
