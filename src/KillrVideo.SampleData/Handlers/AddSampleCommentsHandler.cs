using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DryIocAttributes;
using KillrVideo.Comments;
using KillrVideo.MessageBus;
using KillrVideo.Protobuf;
using KillrVideo.SampleData.Components;
using NLipsum.Core;
using Serilog;

namespace KillrVideo.SampleData.Handlers
{
    /// <summary>
    /// Adds sample comments to videos on the site.
    /// </summary>
    [ExportMany, Reuse(ReuseType.Transient)]
    public class AddSampleCommentsHandler : IHandleMessage<AddSampleCommentsRequest>
    {
        private static readonly ILogger Logger = Log.ForContext<AddSampleCommentsHandler>();

        private static readonly Func<string>[] LipsumSources = new Func<string>[]
        {
            () => Lipsums.ChildHarold,
            () => Lipsums.Decameron,
            () => Lipsums.Faust,
            () => Lipsums.InDerFremde,
            () => Lipsums.LeBateauIvre,
            () => Lipsums.LeMasque,
            () => Lipsums.NagyonFaj,
            () => Lipsums.Omagyar,
            () => Lipsums.RobinsonoKruso,
            () => Lipsums.TheRaven,
            () => Lipsums.TierrayLuna
        };

        private readonly IGetSampleData _sampleDataRetriever;
        private readonly IServiceClientFactory _clientFactory;
        

        public AddSampleCommentsHandler(IGetSampleData sampleDataRetriever, IServiceClientFactory clientFactory)
        {
            if (sampleDataRetriever == null) throw new ArgumentNullException(nameof(sampleDataRetriever));
            if (clientFactory == null) throw new ArgumentNullException(nameof(clientFactory));
            _sampleDataRetriever = sampleDataRetriever;
            _clientFactory = clientFactory;
        }

        public async Task Handle(AddSampleCommentsRequest busCommand)
        {
            // Get some sample users to use as comment authors
            List<Guid> userIds = await _sampleDataRetriever.GetRandomSampleUserIds(busCommand.NumberOfComments).ConfigureAwait(false);
            if (userIds.Count == 0)
            {
                Logger.Warning("No sample users available, cannot add sample comments");
                return;
            }

            // Get some videos to comment on
            List<Guid> videoIds = await _sampleDataRetriever.GetRandomVideoIds(busCommand.NumberOfComments).ConfigureAwait(false);
            if (videoIds.Count == 0)
            {
                Logger.Warning("No sample videos available, cannot add sample comments");
                return;
            }

            // Choose a NLipsum generator for generating the comment text
            int lipsumIdx = new Random().Next(LipsumSources.Length);
            var lipsum = new LipsumGenerator(LipsumSources[lipsumIdx](), false);
            var comments = lipsum.GenerateParagraphs(busCommand.NumberOfComments, Paragraph.Short);

            // Get a client for the comments service
            var commentsService = await _clientFactory.GetCommentsClientAsync().ConfigureAwait(false);
            
            // Add some sample comments in parallel
            var commentTasks = new List<Task>();
            for (int i = 0; i < busCommand.NumberOfComments; i++)
            {
                commentTasks.Add(commentsService.CommentOnVideoAsync(new CommentOnVideoRequest
                {
                    CommentId = global::Cassandra.TimeUuid.NewId().ToGuid().ToTimeUuid(),
                    VideoId = videoIds[i].ToUuid(),
                    UserId = userIds[i].ToUuid(),
                    Comment = comments[i]
                }).ResponseAsync);
            }

            await Task.WhenAll(commentTasks).ConfigureAwait(false);
        }
    }
}
