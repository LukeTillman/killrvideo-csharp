using System;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.SampleData.Dtos;
using KillrVideo.Utils;

namespace KillrVideo.SampleData.Worker.Scheduler.Jobs
{
    /// <summary>
    /// Scheduled job that runs every 15 minutes to add sample users to the site.
    /// </summary>
    public class AddSampleUsersJob : SampleDataJob
    {
        private readonly ISampleDataService _sampleDataService;

        public AddSampleUsersJob(ISession session, TaskCache<string, PreparedStatement> statementCache, ISampleDataService sampleDataService) 
            : base(session, statementCache)
        {
            if (sampleDataService == null) throw new ArgumentNullException("sampleDataService");
            _sampleDataService = sampleDataService;
        }

        protected override int MinutesBetweenRuns
        {
            get { return 15; }
        }

        protected override Task RunImpl()
        {
            return _sampleDataService.AddSampleUsers(new AddSampleUsers { NumberOfUsers = 3, Timestamp = DateTimeOffset.UtcNow });
        }
    }
}