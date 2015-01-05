using System;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.SampleData.Dtos;
using KillrVideo.Utils;

namespace KillrVideo.SampleData.Worker.Scheduler.Jobs
{
    /// <summary>
    /// Scheduled sample data job that adds sample YouTube videos to the site.
    /// </summary>
    public class AddSampleYouTubeVideosJob : SampleDataJob
    {
        private readonly ISampleDataService _sampleDataService;

        /// <summary>
        /// Runs every 8 hours.
        /// </summary>
        protected override int MinutesBetweenRuns
        {
            get { return 480; }
        }

        public AddSampleYouTubeVideosJob(ISession session, TaskCache<string, PreparedStatement> statementCache, ISampleDataService sampleDataService) 
            : base(session, statementCache)
        {
            if (sampleDataService == null) throw new ArgumentNullException("sampleDataService");
            _sampleDataService = sampleDataService;
        }

        protected override Task RunImpl()
        {
            return _sampleDataService.AddSampleYouTubeVideos(new AddSampleYouTubeVideos { NumberOfVideos = 1 });
        }
    }
}