using System;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.MessageBus;
using KillrVideo.Utils;

namespace KillrVideo.SampleData.Scheduler.Jobs
{
    /// <summary>
    /// Job to trigger a refresh of sample YouTube videos available from our list of sample video sources.
    /// </summary>
    public class RefreshYouTubeVideoSourcesJob : SampleDataJob
    {
        private readonly IBus _bus;

        /// <summary>
        /// Runs every 6 hours.
        /// </summary>
        protected override int MinutesBetweenRuns => 360;

        public RefreshYouTubeVideoSourcesJob(ISession session, TaskCache<string, PreparedStatement> statementCache, IBus bus) 
            : base(session, statementCache)
        {
            if (bus == null) throw new ArgumentNullException(nameof(bus));
            _bus = bus;
        }

        protected override Task RunImpl()
        {
            return _bus.Publish(new RefreshYouTubeSourcesRequest());
        }
    }
}
