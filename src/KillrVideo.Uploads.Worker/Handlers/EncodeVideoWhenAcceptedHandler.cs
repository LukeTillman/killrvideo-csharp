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

            // Create a job with a single task to encode the video
            string outputAssetName = string.Format("{0}{1}", UploadConfig.EncodedVideoAssetNamePrefix, filename);
            IJob job = _cloudMediaContext.Jobs.CreateWithSingleTask(MediaProcessorNames.WindowsAzureMediaEncoder,
                                                                    MediaEncoderTaskPresetStrings.H264BroadbandSD16x9, asset,
                                                                    outputAssetName, AssetCreationOptions.None);

            // Get a reference to the asset for the encoded file
            IAsset encodedAsset = job.Tasks.Single().OutputAssets.Single();

            // Add a task to create thumbnails to the encoding job
            string taskName = string.Format("Create Thumbnails - {0}", filename);
            IMediaProcessor processor = _cloudMediaContext.MediaProcessors.GetLatestMediaProcessorByName(MediaProcessorNames.WindowsAzureMediaEncoder);
            ITask task = job.Tasks.AddNew(taskName, processor, UploadConfig.ThumbnailGenerationXml, TaskOptions.ProtectedConfiguration);

            // The task should use the encoded file from the first task as input and output thumbnails in a new asset
            task.InputAssets.Add(encodedAsset);
            task.OutputAssets.AddNew(string.Format("{0}{1}", UploadConfig.ThumbnailAssetNamePrefix, filename), AssetCreationOptions.None);

            // Get status upades on the job's progress on Azure queue, then start the job
            job.JobNotificationSubscriptions.AddNew(NotificationJobState.All, _notificationEndPoint);
            await job.SubmitAsync().ConfigureAwait(false);

            string jobId = job.Id;

            // Store the job information for the video in Cassandra
            PreparedStatement[] saveJobInfoPrepared = await _statementCache.NoContext.GetOrAddAllAsync(
                "INSERT INTO uploaded_video_jobs (videoid, upload_url, jobid) VALUES (?, ?, ?) USING TIMESTAMP ?",
                "INSERT INTO uploaded_video_jobs_by_jobid (jobid, videoid, upload_url) VALUES (?, ?, ?) USING TIMESTAMP ?");

            DateTimeOffset timestamp = DateTimeOffset.UtcNow;

            var batch = new BatchStatement();
            batch.Add(saveJobInfoPrepared[0].Bind(uploadAccepted.VideoId, uploadAccepted.UploadUrl, jobId, timestamp.ToMicrosecondsSinceEpoch()));
            batch.Add(saveJobInfoPrepared[1].Bind(jobId, uploadAccepted.VideoId, uploadAccepted.UploadUrl, timestamp.ToMicrosecondsSinceEpoch()));

            await _session.ExecuteAsync(batch).ConfigureAwait(false);

            // Tell the world an encoding job started
            await _bus.Publish(new UploadedVideoProcessingStarted
            {
                VideoId = uploadAccepted.VideoId,
                Timestamp = timestamp
            }).ConfigureAwait(false);
        }

        // ReSharper restore ReplaceWithSingleCallToFirstOrDefault
    }
}
