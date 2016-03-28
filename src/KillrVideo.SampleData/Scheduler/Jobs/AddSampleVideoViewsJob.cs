using System;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.MessageBus;
using KillrVideo.Utils;

namespace KillrVideo.SampleData.Scheduler.Jobs
{
    /// <summary>
    /// Scheduled job that runs to add video views to the site.
    /// </summary>
    public class AddSampleVideoViewsJob : SampleDataJob
    {
        private readonly IBus _bus;

        /// <summary>
        /// Runs every minute.
        /// </summary>
        protected override int MinutesBetweenRuns => 1;

        public AddSampleVideoViewsJob(ISession session, IBus bus) 
            : base(session)
        {
            if (bus == null) throw new ArgumentNullException(nameof(bus));
            _bus = bus;
        }

        protected override Task RunImpl()
        {
            return _bus.Publish(new AddSampleVideoViewsRequest { NumberOfViews = 100 });
        }
    }
}