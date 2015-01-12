using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.Comments;
using KillrVideo.Comments.Dtos;
using KillrVideo.SampleData.Dtos;
using KillrVideo.SampleData.Worker.Components;
using log4net;
using Nimbus.Handlers;
using NLipsum.Core;

namespace KillrVideo.SampleData.Worker.Handlers
{
    /// <summary>
    /// Adds sample comments to videos on the site.
    /// </summary>
    public class AddSampleCommentsHandler : IHandleCommand<AddSampleComments>
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (AddSampleCommentsHandler));

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
        private readonly ICommentsService _commentService;

        public AddSampleCommentsHandler(IGetSampleData sampleDataRetriever, ICommentsService commentService)
        {
            if (sampleDataRetriever == null) throw new ArgumentNullException("sampleDataRetriever");
            if (commentService == null) throw new ArgumentNullException("commentService");
            _sampleDataRetriever = sampleDataRetriever;
            _commentService = commentService;
        }

        public async Task Handle(AddSampleComments busCommand)
        {
            // Get some sample users to use as comment authors
            List<Guid> userIds = await _sampleDataRetriever.GetRandomSampleUserIds(busCommand.NumberOfComments).ConfigureAwait(false);
            if (userIds.Count == 0)
            {
                Logger.Warn("No sample users available.  Cannot add sample comments.");
                return;
            }

            // Get some videos to comment on
            List<Guid> videoIds = await _sampleDataRetriever.GetRandomVideoIds(busCommand.NumberOfComments).ConfigureAwait(false);
            if (videoIds.Count == 0)
            {
                Logger.Warn("No sample videos available.  Cannot add sample comments.");
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
                commentTasks.Add(_commentService.CommentOnVideo(new CommentOnVideo
                {
                    CommentId = TimeUuid.NewId(),
                    VideoId = videoIds[i],
                    UserId = userIds[i],
                    Comment = comments[i]
                }));
            }

            await Task.WhenAll(commentTasks).ConfigureAwait(false);
        }
    }
}
