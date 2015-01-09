using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Faker;
using KillrVideo.Comments;
using KillrVideo.Comments.Dtos;
using KillrVideo.SampleData.Dtos;
using KillrVideo.SampleData.Worker.Components;
using Nimbus.Handlers;

namespace KillrVideo.SampleData.Worker.Handlers
{
    /// <summary>
    /// Adds sample comments to videos on the site.
    /// </summary>
    public class AddSampleCommentsHandler : IHandleCommand<AddSampleComments>
    {
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
            List<Guid> sampleUserIds = await _sampleDataRetriever.GetRandomSampleUserIds(busCommand.NumberOfComments).ConfigureAwait(false);

            // Get some videos to comment on
            List<Guid> videoIds = await _sampleDataRetriever.GetRandomVideoIds(busCommand.NumberOfComments).ConfigureAwait(false);

            // Add some sample comments in parallel
            var commentTasks = new List<Task>();
            for (int i = 0; i < busCommand.NumberOfComments; i++)
            {
                commentTasks.Add(_commentService.CommentOnVideo(new CommentOnVideo
                {
                    CommentId = Guid.NewGuid(),
                    VideoId = videoIds[i],
                    UserId = sampleUserIds[i],
                    Comment = Lorem.GetParagraph()
                }));
            }

            await Task.WhenAll(commentTasks).ConfigureAwait(false);
        }
    }
}
