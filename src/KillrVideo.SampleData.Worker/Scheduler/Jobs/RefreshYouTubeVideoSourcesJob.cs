using System;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.SampleData.Dtos;
using KillrVideo.Utils;

namespace KillrVideo.SampleData.Worker.Scheduler.Jobs
{
    /// <summary>
    /// Job to trigger a refresh of sample YouTube videos available from our list of sample video sources.
    /// </summary>
    public class RefreshYouTubeVideoSourcesJob : SampleDataJob
    {
        private readonly ISampleDataService _sampleDataService;

        /// <summary>
        /// Runs every 6 hours.
        /// </summary>
        protected override int MinutesBetweenRuns
        {
            get { return 360; }
        }

        public RefreshYouTubeVideoSourcesJob(ISession session, TaskCache<string, PreparedStatement> statementCache, ISampleDataService sampleDataService) 
            : base(session, statementCache)
        {
            if (sampleDataService == null) throw new ArgumentNullException("sampleDataService");
            _sampleDataService = sampleDataService;
        }

        protected override Task RunImpl()
        {
            return _sampleDataService.RefreshYouTubeSources(new RefreshYouTubeSources());
        }
    }
}
