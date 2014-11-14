using System;
using System.Threading.Tasks;
using KillrVideo.VideoCatalog.Messages.Commands;
using Nimbus.Handlers;

namespace KillrVideo.VideoCatalog.Worker.Handlers
{
    /// <summary>
    /// Adds a video to the catalog.
    /// </summary>
    public class AddVideoHandler : IHandleCommand<AddVideo>
    {
        private readonly IVideoCatalogWriteModel _videoCatalog;

        public AddVideoHandler(IVideoCatalogWriteModel videoCatalog)
        {
            if (videoCatalog == null) throw new ArgumentNullException("videoCatalog");
            _videoCatalog = videoCatalog;
        }

        public Task Handle(AddVideo message)
        {
            return _videoCatalog.AddVideo(message);
        }
    }
}
