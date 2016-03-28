using System;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.MessageBus;
using KillrVideo.Utils;

namespace KillrVideo.SampleData.Scheduler.Jobs
{
    /// <summary>
    /// Sample data job that adds sample comments to videos.
    /// </summary>
    public class AddSampleCommentsJob : SampleDataJob
    {
        private readonly IBus _bus;

        /// <summary>
        /// Runs every 5 minutes.
        /// </summary>
        protected override int MinutesBetweenRuns => 5;

        public AddSampleCommentsJob(ISession session, TaskCache<string, PreparedStatement> statementCache, IBus bus) 
            : base(session, statementCache)
        {
            if (bus == null) throw new ArgumentNullException(nameof(bus));
            _bus = bus;
        }

        protected override Task RunImpl()
        {
            return _bus.Publish(new AddSampleCommentsRequest { NumberOfComments = 5 });
        }
    }
}
