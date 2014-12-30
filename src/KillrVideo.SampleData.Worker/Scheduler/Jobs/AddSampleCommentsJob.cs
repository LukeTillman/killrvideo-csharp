using System;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.SampleData.Dtos;
using KillrVideo.Utils;

namespace KillrVideo.SampleData.Worker.Scheduler.Jobs
{
    /// <summary>
    /// Sample data job that runs every 5 minutes to add sample comments to videos.
    /// </summary>
    public class AddSampleCommentsJob : SampleDataJob
    {
        private readonly ISampleDataService _sampleDataService;

        public AddSampleCommentsJob(ISession session, TaskCache<string, PreparedStatement> statementCache, ISampleDataService sampleDataService) 
            : base(session, statementCache)
        {
            if (sampleDataService == null) throw new ArgumentNullException("sampleDataService");
            _sampleDataService = sampleDataService;
        }

        protected override int MinutesBetweenRuns
        {
            get { return 5; }
        }

        protected override Task RunImpl()
        {
            return _sampleDataService.AddSampleComments(new AddSampleComments { NumberOfComments = 5, Timestamp = DateTimeOffset.UtcNow });
        }
    }
}
