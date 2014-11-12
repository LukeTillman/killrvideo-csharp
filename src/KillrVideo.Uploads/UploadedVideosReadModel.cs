using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.Uploads.Dtos;
using KillrVideo.Utils;

namespace KillrVideo.Uploads
{
    /// <summary>
    /// Reads data from Cassandra for uploaded videos.
    /// </summary>
    public class UploadedVideosReadModel : IUploadedVideosReadModel
    {
        private readonly ISession _session;

        private readonly AsyncLazy<PreparedStatement> _getByVideoId;
        private readonly AsyncLazy<PreparedStatement> _getByJobId;
        private readonly AsyncLazy<PreparedStatement> _getStatusForJob; 

        public UploadedVideosReadModel(ISession session)
        {
            if (session == null) throw new ArgumentNullException("session");
            _session = session;

            _getByVideoId = new AsyncLazy<PreparedStatement>(() => _session.PrepareAsync("SELECT * FROM uploaded_videos WHERE videoid = ?"));
            _getByJobId = new AsyncLazy<PreparedStatement>(() => _session.PrepareAsync("SELECT * FROM uploaded_videos_by_jobid WHERE jobid = ?"));
            _getStatusForJob = new AsyncLazy<PreparedStatement>(() => _session.PrepareAsync(
                "SELECT newstate, status_date FROM encoding_job_notifications WHERE jobid = ? LIMIT 1"));
        }

        public async Task<UploadedVideo> GetByVideoId(Guid videoId)
        {
            PreparedStatement preparedStatement = await _getByVideoId;
            RowSet rows = await _session.ExecuteAsync(preparedStatement.Bind(videoId));
            return MapRowToUploadedVideo(rows.SingleOrDefault());
        }

        public async Task<UploadedVideo> GetByJobId(string jobId)
        {
            PreparedStatement preparedStatement = await _getByJobId;
            RowSet rows = await _session.ExecuteAsync(preparedStatement.Bind(jobId));
            return MapRowToUploadedVideo(rows.SingleOrDefault());
        }

        public async Task<EncodingJobProgress> GetStatusForJob(string jobId)
        {
            PreparedStatement preparedStatement = await _getStatusForJob;
            RowSet rows = await _session.ExecuteAsync(preparedStatement.Bind(jobId));
            return MapRowToEncodingJobProgress(rows.SingleOrDefault());
        }

        private static UploadedVideo MapRowToUploadedVideo(Row row)
        {
            if (row == null) return null;

            var tags = row.GetValue<IEnumerable<string>>("tags");
            return new UploadedVideo
            {
                VideoId = row.GetValue<Guid>("videoid"),
                UserId = row.GetValue<Guid>("userid"),
                Name = row.GetValue<string>("name"),
                Description = row.GetValue<string>("description"),
                Tags = tags == null ? new HashSet<string>() : new HashSet<string>(tags),
                AddedDate = row.GetValue<DateTimeOffset>("added_date"),
                JobId = row.GetValue<string>("jobid")
            };
        }

        private static EncodingJobProgress MapRowToEncodingJobProgress(Row row)
        {
            if (row == null) return null;

            return new EncodingJobProgress
            {
                CurrentState = row.GetValue<string>("newstate"),
                StatusDate = row.GetValue<DateTimeOffset>("status_date")
            };
        }
    }
}