using System;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.SampleData.Dtos;
using KillrVideo.Utils;

namespace KillrVideo.SampleData.Worker.Scheduler.Jobs
{
    /// <summary>
    /// Scheduled job that runs to add video views to the site.
    /// </summary>
    public class AddSampleVideoViewsJob : SampleDataJob
    {
        private readonly ISampleDataService _sampleDataService;

        /// <summary>
        /// Runs every minute.
        /// </summary>
        protected override int MinutesBetweenRuns
        {
            get { return 1; }
        }

        public AddSampleVideoViewsJob(ISession session, TaskCache<string, PreparedStatement> statementCache, ISampleDataService sampleDataService) 
            : base(session, statementCache)
        {
            if (sampleDataService == null) throw new ArgumentNullException("sampleDataService");
            _sampleDataService = sampleDataService;
        }

        protected override Task RunImpl()
        {
            return _sampleDataService.AddSampleVideoViews(new AddSampleVideoViews { NumberOfViews = 100 });
        }
    }
}