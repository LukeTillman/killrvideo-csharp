using System;
using System.Threading.Tasks;
using KillrVideo.VideoCatalog.Messages.Events;
using Nimbus.Handlers;

namespace KillrVideo.Uploads.Worker.Handlers
{
    /// <summary>
    /// Handler for kicking off an encoding job once an uploaded video has been accepted.
    /// </summary>
    public class EncodeVideoWhenAcceptedHandler : IHandleMulticastEvent<UploadedVideoAccepted>
    {
        private readonly IManageEncodingJobs _encodingJobManager;

        public EncodeVideoWhenAcceptedHandler(IManageEncodingJobs encodingJobManager)
        {
            if (encodingJobManager == null) throw new ArgumentNullException("encodingJobManager");
            _encodingJobManager = encodingJobManager;
        }

        public Task Handle(UploadedVideoAccepted busEvent)
        {
            return _encodingJobManager.StartEncodingJob(busEvent.VideoId, busEvent.UploadUrl);
        }
    }
}
