using System;
using System.Threading.Tasks;
using KillrVideo.VideoCatalog.Messages.Commands;
using Nimbus.Handlers;

namespace KillrVideo.VideoCatalog.Worker.Handlers
{
    /// <summary>
    /// Adds a YouTube video to the catalog.
    /// </summary>
    public class SubmitYouTubeVideoHandler : IHandleCommand<SubmitYouTubeVideo>
    {
        private readonly IVideoCatalogWriteModel _videoCatalog;

        public SubmitYouTubeVideoHandler(IVideoCatalogWriteModel videoCatalog)
        {
            if (videoCatalog == null) throw new ArgumentNullException("videoCatalog");
            _videoCatalog = videoCatalog;
        }
        
        public Task Handle(SubmitYouTubeVideo busCommand)
        {
            return _videoCatalog.AddYouTubeVideo(busCommand);
        }
    }
}
