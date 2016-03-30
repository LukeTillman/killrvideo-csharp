using System;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.Cassandra;
using KillrVideo.MessageBus;

namespace KillrVideo.SampleData.Scheduler.Jobs
{
    /// <summary>
    /// Scheduled job that runs to add sample users to the site.
    /// </summary>
    public class AddSampleUsersJob : SampleDataJob
    {
        private readonly IBus _bus;

        /// <summary>
        /// Runs every 2 hours.
        /// </summary>
        protected override int MinutesBetweenRuns => 120;

        public AddSampleUsersJob(ISession session, PreparedStatementCache statementCache, IBus bus) 
            : base(session, statementCache)
        {
            if (bus == null) throw new ArgumentNullException(nameof(bus));
            _bus = bus;
        }

        protected override Task RunImpl()
        {
            int numberOfUsers = IsFirstTimeRunning ? 10 : 3;
            return _bus.Publish(new AddSampleUsersRequest { NumberOfUsers = numberOfUsers });
        }
    }
}