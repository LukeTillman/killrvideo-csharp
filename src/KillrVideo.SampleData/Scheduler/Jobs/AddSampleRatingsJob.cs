using System;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.MessageBus;
using KillrVideo.Utils;

namespace KillrVideo.SampleData.Scheduler.Jobs
{
    /// <summary>
    /// Scheduled job that runs to add sample ratings to videos.
    /// </summary>
    public class AddSampleRatingsJob : SampleDataJob
    {
        private readonly IBus _bus;

        /// <summary>
        /// Runs every 2 minutes.
        /// </summary>
        protected override int MinutesBetweenRuns => 2;

        public AddSampleRatingsJob(ISession session, TaskCache<string, PreparedStatement> statementCache, IBus bus) 
            : base(session, statementCache)
        {
            if (bus == null) throw new ArgumentNullException(nameof(bus));
            _bus = bus;
        }

        protected override Task RunImpl()
        {
            return _bus.Publish(new AddSampleRatingsRequest { NumberOfRatings = 5 });
        }
    }
}