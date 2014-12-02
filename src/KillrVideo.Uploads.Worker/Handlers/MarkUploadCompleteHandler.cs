using System;
using System.Threading.Tasks;
using KillrVideo.Uploads.Messages.Commands;
using Nimbus.Handlers;

namespace KillrVideo.Uploads.Worker.Handlers
{
    /// <summary>
    /// Marks uploads as complete.
    /// </summary>
    public class MarkUploadCompleteHandler : IHandleCommand<MarkUploadComplete>
    {
        private readonly IManageUploadDestinations _uploadDestinationManager;

        public MarkUploadCompleteHandler(IManageUploadDestinations uploadDestinationManager)
        {
            if (uploadDestinationManager == null) throw new ArgumentNullException("uploadDestinationManager");
            _uploadDestinationManager = uploadDestinationManager;
        }

        public Task Handle(MarkUploadComplete busCommand)
        {
            return _uploadDestinationManager.MarkUploadComplete(busCommand.UploadUrl);
        }
    }
}