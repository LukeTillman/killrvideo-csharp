using System;
using KillrVideo.Comments.Api.Commands;
using Rebus;

namespace KillrVideo.Comments.Worker.Handlers
{
    /// <summary>
    /// Handles recording user comments on videos.
    /// </summary>
    public class CommentOnVideoHandler : IHandleMessages<CommentOnVideo>
    {
        private readonly ICommentWriteModel _commentWriteModel;

        public CommentOnVideoHandler(ICommentWriteModel commentWriteModel)
        {
            if (commentWriteModel == null) throw new ArgumentNullException("commentWriteModel");
            _commentWriteModel = commentWriteModel;
        }

        public void Handle(CommentOnVideo message)
        {
            _commentWriteModel.CommentOnVideo(message);
        }
    }
}
