using System;
using System.Threading.Tasks;
using KillrVideo.VideoCatalog.Messages.Commands;
using Nimbus.Handlers;

namespace KillrVideo.VideoCatalog.Worker.Handlers
{
    /// <summary>
    /// Adds an uploaded video to the catalog.
    /// </summary>
    public class SubmitUploadedVideoHandler : IHandleCommand<SubmitUploadedVideo>
    {
        private readonly IVideoCatalogWriteModel _videoCatalog;

        public SubmitUploadedVideoHandler(IVideoCatalogWriteModel videoCatalog)
        {
            if (videoCatalog == null) throw new ArgumentNullException("videoCatalog");
            _videoCatalog = videoCatalog;
        }

        public Task Handle(SubmitUploadedVideo busCommand)
        {
            return _videoCatalog.AddUploadedVideo(busCommand);
        }
    }
}