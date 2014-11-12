using System;
using KillrVideo.Uploads.Messages.Commands;
using Rebus;

namespace KillrVideo.Uploads.Worker.Handlers
{
    /// <summary>
    /// Adds an uploaded video.
    /// </summary>
    public class AddUploadedVideoHandler : IHandleMessages<AddUploadedVideo>
    {
        private readonly IUploadedVideosWriteModel _uploadWriteModel;

        public AddUploadedVideoHandler(IUploadedVideosWriteModel uploadWriteModel)
        {
            if (uploadWriteModel == null) throw new ArgumentNullException("uploadWriteModel");
            _uploadWriteModel = uploadWriteModel;
        }

        public void Handle(AddUploadedVideo message)
        {
            _uploadWriteModel.AddVideo(message);
        }
    }
}
