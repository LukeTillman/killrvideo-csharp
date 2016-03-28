using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
        private readonly CommentsService.ICommentsServiceClient _commentService;

        public AddSampleCommentsHandler(IGetSampleData sampleDataRetriever, CommentsService.ICommentsServiceClient commentService)
        {
            if (sampleDataRetriever == null) throw new ArgumentNullException(nameof(sampleDataRetriever));
            if (commentService == null) throw new ArgumentNullException(nameof(commentService));
            _sampleDataRetriever = sampleDataRetriever;
            _commentService = commentService;
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
            
            // Add some sample comments in parallel
            var commentTasks = new List<Task>();
            for (int i = 0; i < busCommand.NumberOfComments; i++)
            {
                commentTasks.Add(_commentService.CommentOnVideoAsync(new CommentOnVideoRequest
                {
                    CommentId = Cassandra.TimeUuid.NewId().ToGuid().ToTimeUuid(),
                    VideoId = videoIds[i].ToUuid(),
                    UserId = userIds[i].ToUuid(),
                    Comment = comments[i]
                }).ResponseAsync);
            }

            await Task.WhenAll(commentTasks).ConfigureAwait(false);
        }
    }
}
