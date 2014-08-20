using System;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.Data.Upload.Dtos;

namespace KillrVideo.Data.Upload
{
    /// <summary>
    /// Reads data from Cassandra for uploaded videos.
    /// </summary>
    public class UploadedVideosReadModel : IUploadedVideosReadModel
    {
        private readonly ISession _session;

        public UploadedVideosReadModel(ISession session)
        {
            if (session == null) throw new ArgumentNullException("session");
            _session = session;
        }

        public Task<UploadedVideo> GetByVideoId(Guid videoId)
        {
            throw new NotImplementedException();
        }

        public Task<UploadedVideo> GetByJobId(string jobId)
        {
            throw new NotImplementedException();
        }

        public Task<EncodingJobProgress> GetStatusForJob(string jobId)
        {
            throw new NotImplementedException();
        }
    }
}