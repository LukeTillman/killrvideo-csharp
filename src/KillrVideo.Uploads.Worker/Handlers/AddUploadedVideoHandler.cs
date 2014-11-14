using System;
using System.Threading.Tasks;
using KillrVideo.Uploads.Messages.Commands;
using Nimbus.Handlers;

namespace KillrVideo.Uploads.Worker.Handlers
{
    /// <summary>
    /// Adds an uploaded video.
    /// </summary>
    public class AddUploadedVideoHandler : IHandleCommand<AddUploadedVideo>
    {
        private readonly IUploadedVideosWriteModel _uploadWriteModel;

        public AddUploadedVideoHandler(IUploadedVideosWriteModel uploadWriteModel)
        {
            if (uploadWriteModel == null) throw new ArgumentNullException("uploadWriteModel");
            _uploadWriteModel = uploadWriteModel;
        }

        public Task Handle(AddUploadedVideo message)
        {
            return _uploadWriteModel.AddVideo(message);
        }
    }
}
