using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DryIocAttributes;
using KillrVideo.MessageBus;
using KillrVideo.Protobuf;
using KillrVideo.SampleData.Components;
using KillrVideo.SampleData.Components.YouTube;
using KillrVideo.VideoCatalog;
using Serilog;

namespace KillrVideo.SampleData.Handlers
{
    /// <summary>
    /// Adds sample YouTube videos to the site.
    /// </summary>
    [ExportMany, Reuse(ReuseType.Transient)]
    public class AddSampleYouTubeVideosHandler : IHandleMessage<AddSampleYouTubeVideosRequest>
    {
        private static readonly ILogger Logger = Log.ForContext<AddSampleYouTubeVideosHandler>();

        private readonly IGetSampleData _sampleDataRetriever;
        private readonly IManageSampleYouTubeVideos _youTubeManager;
        private readonly IServiceClientFactory _clientFactory;

        public AddSampleYouTubeVideosHandler(IGetSampleData sampleDataRetriever, IManageSampleYouTubeVideos youTubeManager,
                                             IServiceClientFactory clientFactory)
        {
            if (sampleDataRetriever == null) throw new ArgumentNullException(nameof(sampleDataRetriever));
            if (youTubeManager == null) throw new ArgumentNullException(nameof(youTubeManager));
            if (clientFactory == null) throw new ArgumentNullException(nameof(clientFactory));

            _sampleDataRetriever = sampleDataRetriever;
            _youTubeManager = youTubeManager;
            _clientFactory = clientFactory;
        }

        public async Task Handle(AddSampleYouTubeVideosRequest busCommand)
        {
            // Get some sample users to be the authors for the videos we're going to add
            List<Guid> userIds = await _sampleDataRetriever.GetRandomSampleUserIds(busCommand.NumberOfVideos).ConfigureAwait(false);
            if (userIds.Count == 0)
            {
                Logger.Warning("No sample users available, cannot add sample YouTube videos");
                return;
            }

            // Get some unused sample videos
            List<YouTubeVideo> sampleVideos = await _youTubeManager.GetUnusedVideos(busCommand.NumberOfVideos).ConfigureAwait(false);

            // Get video catalog client
            var videoCatalog = await _clientFactory.GetVideoClientAsync().ConfigureAwait(false);

            // Add them to the site using sample users
            for (int idx = 0; idx < sampleVideos.Count; idx++)
            {
                YouTubeVideo sampleVideo = sampleVideos[idx];
                Guid userId = userIds[idx];

                var request = new SubmitYouTubeVideoRequest
                {
                    VideoId = Guid.NewGuid().ToUuid(),
                    UserId = userId.ToUuid(),
                    YouTubeVideoId = sampleVideo.YouTubeVideoId,
                    Name = sampleVideo.Name,
                    Description = sampleVideo.Description
                };
                request.Tags.Add(sampleVideo.SuggestedTags);
                
                // Submit the video
                await videoCatalog.SubmitYouTubeVideoAsync(request).ResponseAsync.ConfigureAwait(false);

                // Mark them as used so we make a best effort not to reuse sample videos and post duplicates
                await _youTubeManager.MarkVideoAsUsed(sampleVideo).ConfigureAwait(false);
            }
        }
    }
}