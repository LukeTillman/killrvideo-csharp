using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KillrVideo.SampleData.Dtos;
using KillrVideo.SampleData.Worker.Components;
using KillrVideo.Statistics;
using KillrVideo.Statistics.Dtos;
using Nimbus.Handlers;

namespace KillrVideo.SampleData.Worker.Handlers
{
    /// <summary>
    /// Adds sample video views to the site.
    /// </summary>
    public class AddSampleVideoViewsHandler : IHandleCommand<AddSampleVideoViews>
    {
        private readonly IGetSampleData _sampleDataRetriever;
        private readonly IStatisticsService _statsService;

        public AddSampleVideoViewsHandler(IGetSampleData sampleDataRetriever, IStatisticsService statsService)
        {
            if (sampleDataRetriever == null) throw new ArgumentNullException("sampleDataRetriever");
            if (statsService == null) throw new ArgumentNullException("statsService");
            _sampleDataRetriever = sampleDataRetriever;
            _statsService = statsService;
        }

        public async Task Handle(AddSampleVideoViews busCommand)
        {
            // Get some videos for adding views to
            List<Guid> videoIds = await _sampleDataRetriever.GetRandomVideoIds(busCommand.NumberOfViews).ConfigureAwait(false);

            // Add some views in parallel
            var viewTasks = new List<Task>();
            for (int i = 0; i < busCommand.NumberOfViews; i++)
            {
                viewTasks.Add(_statsService.RecordPlaybackStarted(new RecordPlaybackStarted
                {
                    VideoId = videoIds[i]
                }));
            }

            await Task.WhenAll(viewTasks).ConfigureAwait(false);
        }
    }
}