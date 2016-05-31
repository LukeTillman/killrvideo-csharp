using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DryIocAttributes;
using KillrVideo.MessageBus;
using KillrVideo.Protobuf;
using KillrVideo.SampleData.Components;
using KillrVideo.Statistics;
using Serilog;

namespace KillrVideo.SampleData.Handlers
{
    /// <summary>
    /// Adds sample video views to the site.
    /// </summary>
    [ExportMany, Reuse(ReuseType.Transient)]
    public class AddSampleVideoViewsHandler : IHandleMessage<AddSampleVideoViewsRequest>
    {
        private static readonly ILogger Logger = Log.ForContext<AddSampleVideoViewsHandler>();

        private readonly IGetSampleData _sampleDataRetriever;
        private readonly IServiceClientFactory _clientFactory;

        public AddSampleVideoViewsHandler(IGetSampleData sampleDataRetriever, IServiceClientFactory clientFactory)
        {
            if (sampleDataRetriever == null) throw new ArgumentNullException(nameof(sampleDataRetriever));
            if (clientFactory == null) throw new ArgumentNullException(nameof(clientFactory));
            _sampleDataRetriever = sampleDataRetriever;
            _clientFactory = clientFactory;
        }

        public async Task Handle(AddSampleVideoViewsRequest busCommand)
        {
            // Get some videos for adding views to
            List<Guid> videoIds = await _sampleDataRetriever.GetRandomVideoIds(busCommand.NumberOfViews).ConfigureAwait(false);
            if (videoIds.Count == 0)
            {
                Logger.Warning("No sample videos available, cannot add sample video views");
                return;
            }

            // Get stats client
            var statsService = await _clientFactory.GetStatsClientAsync().ConfigureAwait(false);

            // Add some views in parallel
            var viewTasks = new List<Task>();
            for (int i = 0; i < busCommand.NumberOfViews; i++)
            {
                viewTasks.Add(statsService.RecordPlaybackStartedAsync(new RecordPlaybackStartedRequest
                {
                    VideoId = videoIds[i].ToUuid()
                }).ResponseAsync);
            }

            await Task.WhenAll(viewTasks).ConfigureAwait(false);
        }
    }
}