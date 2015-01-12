using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cassandra;
using Faker;
using KillrVideo.Comments;
using KillrVideo.Comments.Dtos;
using KillrVideo.SampleData.Dtos;
using KillrVideo.SampleData.Worker.Components;
using log4net;
using Nimbus.Handlers;

namespace KillrVideo.SampleData.Worker.Handlers
{
    /// <summary>
    /// Adds sample comments to videos on the site.
    /// </summary>
    public class AddSampleCommentsHandler : IHandleCommand<AddSampleComments>
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (AddSampleCommentsHandler));

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

            // Add some sample comments in parallel
            var commentTasks = new List<Task>();
            for (int i = 0; i < busCommand.NumberOfComments; i++)
            {
                commentTasks.Add(_commentService.CommentOnVideo(new CommentOnVideo
                {
                    CommentId = TimeUuid.NewId(),
                    VideoId = videoIds[i],
                    UserId = userIds[i],
                    Comment = Lorem.GetParagraph()
                }));
            }

            await Task.WhenAll(commentTasks).ConfigureAwait(false);
        }
    }
}
