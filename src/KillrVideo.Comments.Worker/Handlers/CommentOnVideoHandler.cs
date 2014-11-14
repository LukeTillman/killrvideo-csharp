using System;
using System.Threading.Tasks;
using KillrVideo.Comments.Messages.Commands;
using Nimbus.Handlers;

namespace KillrVideo.Comments.Worker.Handlers
{
    /// <summary>
    /// Handles recording user comments on videos.
    /// </summary>
    public class CommentOnVideoHandler : IHandleCommand<CommentOnVideo>
    {
        private readonly ICommentWriteModel _commentWriteModel;

        public CommentOnVideoHandler(ICommentWriteModel commentWriteModel)
        {
            if (commentWriteModel == null) throw new ArgumentNullException("commentWriteModel");
            _commentWriteModel = commentWriteModel;
        }

        public Task Handle(CommentOnVideo message)
        {
            return _commentWriteModel.CommentOnVideo(message);
        }
    }
}
