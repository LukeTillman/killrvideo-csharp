using System;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.Utils;

namespace KillrVideo.SampleData.Scheduler.Jobs
{
    /// <summary>
    /// Sample data job that adds sample comments to videos.
    /// </summary>
    public class AddSampleCommentsJob : SampleDataJob
    {
        private readonly ISampleDataService _sampleDataService;

        /// <summary>
        /// Runs every 5 minutes.
        /// </summary>
        protected override int MinutesBetweenRuns
        {
            get { return 5; }
        }

        public AddSampleCommentsJob(ISession session, TaskCache<string, PreparedStatement> statementCache, ISampleDataService sampleDataService) 
            : base(session, statementCache)
        {
            if (sampleDataService == null) throw new ArgumentNullException("sampleDataService");
            _sampleDataService = sampleDataService;
        }

        protected override Task RunImpl()
        {
            return _sampleDataService.AddSampleComments(new AddSampleComments { NumberOfComments = 5 });
        }
    }
}
