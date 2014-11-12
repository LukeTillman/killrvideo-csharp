using System;
using KillrVideo.VideoCatalog.Api.Commands;
using Rebus;

namespace KillrVideo.VideoCatalog.Worker.Handlers
{
    /// <summary>
    /// Adds a video to the catalog.
    /// </summary>
    public class AddVideoHandler : IHandleMessages<AddVideo>
    {
        private readonly IVideoCatalogWriteModel _videoCatalog;

        public AddVideoHandler(IVideoCatalogWriteModel videoCatalog)
        {
            if (videoCatalog == null) throw new ArgumentNullException("videoCatalog");
            _videoCatalog = videoCatalog;
        }

        public void Handle(AddVideo message)
        {
            _videoCatalog.AddVideo(message);
        }
    }
}
