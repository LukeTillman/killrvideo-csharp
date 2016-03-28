using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.MessageBus;
using KillrVideo.SampleData.Components;
using KillrVideo.SampleData.Components.YouTube;
using KillrVideo.Utils;

namespace KillrVideo.SampleData.Scheduler.Jobs
{
    /// <summary>
    /// Scheduled sample data job that adds sample YouTube videos to the site.
    /// </summary>
    public class AddSampleYouTubeVideosJob : SampleDataJob
    {
        private readonly IBus _bus;
        private readonly IManageSampleYouTubeVideos _youTubeManager;
        private readonly IGetSampleData _sampleDataRetriever;

        /// <summary>
        /// Runs every 8 hours.
        /// </summary>
        protected override int MinutesBetweenRuns => 480;

        public AddSampleYouTubeVideosJob(ISession session, TaskCache<string, PreparedStatement> statementCache, IBus bus,
                                         IManageSampleYouTubeVideos youTubeManager, IGetSampleData sampleDataRetriever)
            : base(session, statementCache)
        {
            if (bus == null) throw new ArgumentNullException(nameof(bus));
            if (youTubeManager == null) throw new ArgumentNullException("youTubeManager");
            if (sampleDataRetriever == null) throw new ArgumentNullException("sampleDataRetriever");

            _bus = bus;
            _youTubeManager = youTubeManager;
            _sampleDataRetriever = sampleDataRetriever;
        }

        protected override async Task RunImpl()
        {
            // When running for the first time, try to make sure we've refreshed videos from YouTube first and added some users before running
            int numberOfVideos = 1;
            if (IsFirstTimeRunning)
            {
                // Will throw if no videos are there yet, causing the job to be delayed and retried
                await _youTubeManager.GetUnusedVideos(10).ConfigureAwait(false);
                
                // Check for users
                List<Guid> userIds = await _sampleDataRetriever.GetRandomSampleUserIds(10).ConfigureAwait(false);
                if (userIds.Count == 0)
                    throw new InvalidOperationException("No sample users available yet.  Waiting to run AddSampleYouTubeVideosJob.");
                    
                // Otherwise, we're good, add 10 sample videos the first time we run
                numberOfVideos = 10;
            }

            await _bus.Publish(new AddSampleYouTubeVideosRequest { NumberOfVideos = numberOfVideos }).ConfigureAwait(false);
        }
    }
}