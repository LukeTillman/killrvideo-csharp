using System;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.Data.Videos.Dtos;

namespace KillrVideo.Data.Videos
{
    /// <summary>
    /// Handles writing uploaded video data to Cassandra.
    /// </summary>
    public class UploadedVideosWriteModel : IUploadedVideosWriteModel
    {
        private readonly ISession _session;

        private readonly AsyncLazy<PreparedStatement[]> _addUploadedVideoStatements;
        private readonly AsyncLazy<PreparedStatement> _addNotificationStatement;

        public UploadedVideosWriteModel(ISession session)
        {
            if (session == null) throw new ArgumentNullException("session");
            _session = session;

            // Multiple statement are involved in adding an uploaded video, so wrap in a single AsyncLazy to wait for all of them being prepared
            _addUploadedVideoStatements = new AsyncLazy<PreparedStatement[]>(() => Task.WhenAll(new[]
            {
                _session.PrepareAsync("INSERT INTO uploaded_videos (videoid, userid, name, description, tags, added_date, jobid) " +
                                      "VALUES (?, ?, ?, ?, ?, ?, ?)"),
                _session.PrepareAsync("INSERT INTO uploaded_videos_by_jobid (jobid, videoid, userid, name, description, tags, added_date) " +
                                      "VALUES (?, ?, ?, ?, ?, ?, ?)")
            }));

            // Only a single prepared statement necessary here
            _addNotificationStatement = new AsyncLazy<PreparedStatement>(() => _session.PrepareAsync(
                "INSERT INTO encoding_job_notifications (jobid, status_date, etag, newstate, oldstate) VALUES (?, ?, ?, ?, ?)"));
        }

        /// <summary>
        /// Adds a new uploaded video.
        /// </summary>
        public async Task AddVideo(AddUploadedVideo video)
        {
            DateTimeOffset addedDate = DateTimeOffset.UtcNow;

            // Use an atomic batch to send the inserts
            var batch = new BatchStatement();

            PreparedStatement[] insertStatements = await _addUploadedVideoStatements;

            // INSERT INTO uploaded_videos ...
            batch.Add(insertStatements[0].Bind(video.VideoId, video.UserId, video.Name, video.Description, video.Tags, addedDate, video.JobId));

            // INSERT INTO uploaded_videos_by_jobid ...
            batch.Add(insertStatements[1].Bind(video.JobId, video.VideoId, video.UserId, video.Name, video.Description, video.Tags, addedDate));

            // Send the batch
            await _session.ExecuteAsync(batch).ConfigureAwait(false);
        }

        /// <summary>
        /// Adds a notification about an encoding job.
        /// </summary>
        public async Task AddEncodingJobNotification(AddEncodingJobNotification notification)
        {
            PreparedStatement preparedStatement = await _addNotificationStatement;

            // INSERT INTO encoding_job_notifications ...
            await _session.ExecuteAsync(preparedStatement.Bind(notification.JobId, notification.StatusDate, notification.ETag, notification.NewState,
                                                               notification.OldState));
        }
    }
}