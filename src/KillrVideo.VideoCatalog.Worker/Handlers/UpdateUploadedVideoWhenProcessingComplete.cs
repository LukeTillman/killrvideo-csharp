using System;
using System.Threading.Tasks;
using KillrVideo.Uploads.Messages.Events;
using Nimbus.Handlers;

namespace KillrVideo.VideoCatalog.Worker.Handlers
{
    /// <summary>
    /// Updates an uploaded videos information in the catalog once the video has been processed and is ready for viewing.
    /// </summary>
    public class UpdateUploadedVideoWhenProcessingComplete : IHandleMulticastEvent<UploadedVideoProcessingSucceeded>
    {
        private readonly IVideoCatalogWriteModel _videoCatalog;

        public UpdateUploadedVideoWhenProcessingComplete(IVideoCatalogWriteModel videoCatalog)
        {
            if (videoCatalog == null) throw new ArgumentNullException("videoCatalog");
            _videoCatalog = videoCatalog;
        }

        public Task Handle(UploadedVideoProcessingSucceeded busEvent)
        {
            return _videoCatalog.UpdateUploadedVideoLocations(busEvent.VideoId, busEvent.VideoUrl, busEvent.ThumbnailUrl);
        }
    }
}