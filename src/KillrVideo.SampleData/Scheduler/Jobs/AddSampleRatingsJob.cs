using System;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.SampleData.Dtos;
using KillrVideo.Utils;

namespace KillrVideo.SampleData.Worker.Scheduler.Jobs
{
    /// <summary>
    /// Scheduled job that runs to add sample ratings to videos.
    /// </summary>
    public class AddSampleRatingsJob : SampleDataJob
    {
        private readonly ISampleDataService _sampleDataService;

        /// <summary>
        /// Runs every 2 minutes.
        /// </summary>
        protected override int MinutesBetweenRuns
        {
            get { return 2; }
        }

        public AddSampleRatingsJob(ISession session, TaskCache<string, PreparedStatement> statementCache, ISampleDataService sampleDataService) 
            : base(session, statementCache)
        {
            if (sampleDataService == null) throw new ArgumentNullException("sampleDataService");
            _sampleDataService = sampleDataService;
        }

        protected override Task RunImpl()
        {
            return _sampleDataService.AddSampleRatings(new AddSampleRatings { NumberOfRatings = 5 });
        }
    }
}