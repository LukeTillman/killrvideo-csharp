using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DryIocAttributes;
using KillrVideo.MessageBus;
using KillrVideo.Protobuf;
using KillrVideo.Ratings;
using KillrVideo.SampleData.Components;
using Serilog;

namespace KillrVideo.SampleData.Handlers
{
    /// <summary>
    /// Adds sample video ratings to the site.
    /// </summary>
    [ExportMany, Reuse(ReuseType.Transient)]
    public class AddSampleRatingsHandler : IHandleMessage<AddSampleRatingsRequest>
    {
        private static readonly ILogger Logger = Log.ForContext<AddSampleRatingsHandler>();

        private readonly IGetSampleData _sampleDataRetriever;
        private readonly RatingsService.RatingsServiceClient _ratingsService;

        public AddSampleRatingsHandler(IGetSampleData sampleDataRetriever, RatingsService.RatingsServiceClient ratingsService)
        {
            if (sampleDataRetriever == null) throw new ArgumentNullException(nameof(sampleDataRetriever));
            if (ratingsService == null) throw new ArgumentNullException(nameof(ratingsService));

            _sampleDataRetriever = sampleDataRetriever;
            _ratingsService = ratingsService;
        }

        public async Task Handle(AddSampleRatingsRequest busCommand)
        {
            // Get some user ids and video ids to rate with those users
            List<Guid> userIds = await _sampleDataRetriever.GetRandomSampleUserIds(busCommand.NumberOfRatings).ConfigureAwait(false);
            if (userIds.Count == 0)
            {
                Logger.Warning("No sample users available, cannot add sample ratings");
                return;
            }

            List<Guid> videoIds = await _sampleDataRetriever.GetRandomVideoIds(busCommand.NumberOfRatings).ConfigureAwait(false);
            if (videoIds.Count == 0)
            {
                Logger.Warning("No sample videos available, cannot add sample ratings");
                return;
            }

            // Rate some videos in parallel
            var ratingTasks = new List<Task>();
            var random = new Random();
            for (int i = 0; i < busCommand.NumberOfRatings; i++)
            {
                ratingTasks.Add(_ratingsService.RateVideoAsync(new RateVideoRequest
                {
                    UserId = userIds[i].ToUuid(), 
                    VideoId = videoIds[i].ToUuid(), 
                    Rating = random.Next(1, 6)
                }).ResponseAsync);
            }

            await Task.WhenAll(ratingTasks).ConfigureAwait(false);
        }
    }
}