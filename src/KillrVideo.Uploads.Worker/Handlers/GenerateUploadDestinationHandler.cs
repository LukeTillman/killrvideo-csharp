using System;
using System.Threading.Tasks;
using KillrVideo.Uploads.Messages.RequestResponse;
using Nimbus.Handlers;

namespace KillrVideo.Uploads.Worker.Handlers
{
    /// <summary>
    /// Handles requests to generate upload destinations for uploaded videos.
    /// </summary>
    public class GenerateUploadDestinationHandler : IHandleRequest<GenerateUploadDestination, UploadDestination>
    {
        private readonly IManageUploadDestinations _uploadDestinationManager;

        public GenerateUploadDestinationHandler(IManageUploadDestinations uploadDestinationManager)
        {
            if (uploadDestinationManager == null) throw new ArgumentNullException("uploadDestinationManager");
            _uploadDestinationManager = uploadDestinationManager;
        }

        public Task<UploadDestination> Handle(GenerateUploadDestination request)
        {
            return _uploadDestinationManager.GenerateUploadDestination(request);
        }
    }
}
