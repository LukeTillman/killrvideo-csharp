using System;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.SampleData.Dtos;
using KillrVideo.Utils;

namespace KillrVideo.SampleData.Worker.Scheduler.Jobs
{
    /// <summary>
    /// Scheduled sample data job that adds YouTube videos to the site every 10 minutes.
    /// </summary>
    public class AddSampleYouTubeVideosJob : SampleDataJob
    {
        private readonly ISampleDataService _sampleDataService;

        public AddSampleYouTubeVideosJob(ISession session, TaskCache<string, PreparedStatement> statementCache, ISampleDataService sampleDataService) 
            : base(session, statementCache)
        {
            if (sampleDataService == null) throw new ArgumentNullException("sampleDataService");
            _sampleDataService = sampleDataService;
        }

        protected override int MinutesBetweenRuns
        {
            get { return 10; }
        }

        protected override Task RunImpl()
        {
            return _sampleDataService.AddSampleYouTubeVideos(new AddSampleYouTubeVideos { NumberOfVideos = 3, Timestamp = DateTimeOffset.UtcNow });
        }
    }
}