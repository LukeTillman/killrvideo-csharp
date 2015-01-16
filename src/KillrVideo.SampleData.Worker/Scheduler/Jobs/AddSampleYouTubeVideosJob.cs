using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.SampleData.Dtos;
using KillrVideo.SampleData.Worker.Components;
using KillrVideo.SampleData.Worker.Components.YouTube;
using KillrVideo.Utils;

namespace KillrVideo.SampleData.Worker.Scheduler.Jobs
{
    /// <summary>
    /// Scheduled sample data job that adds sample YouTube videos to the site.
    /// </summary>
    public class AddSampleYouTubeVideosJob : SampleDataJob
    {
        private readonly ISampleDataService _sampleDataService;
        private readonly IManageSampleYouTubeVideos _youTubeManager;
        private readonly IGetSampleData _sampleDataRetriever;

        /// <summary>
        /// Runs every 8 hours.
        /// </summary>
        protected override int MinutesBetweenRuns
        {
            get { return 480; }
        }

        public AddSampleYouTubeVideosJob(ISession session, TaskCache<string, PreparedStatement> statementCache, ISampleDataService sampleDataService,
                                         IManageSampleYouTubeVideos youTubeManager, IGetSampleData sampleDataRetriever)
            : base(session, statementCache)
        {
            if (sampleDataService == null) throw new ArgumentNullException("sampleDataService");
            if (youTubeManager == null) throw new ArgumentNullException("youTubeManager");
            if (sampleDataRetriever == null) throw new ArgumentNullException("sampleDataRetriever");

            _sampleDataService = sampleDataService;
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
                await _youTubeManager.GetUnusedVideos(10);
                
                // Check for users
                List<Guid> userIds = await _sampleDataRetriever.GetRandomSampleUserIds(10);
                if (userIds.Count == 0)
                    throw new InvalidOperationException("No sample users available yet.  Waiting to run AddSampleYouTubeVideosJob.");
                    
                // Otherwise, we're good, add 10 sample videos the first time we run
                numberOfVideos = 10;
            }

            await _sampleDataService.AddSampleYouTubeVideos(new AddSampleYouTubeVideos { NumberOfVideos = numberOfVideos }).ConfigureAwait(false);
        }
    }
}