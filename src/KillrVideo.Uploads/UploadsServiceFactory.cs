using System.ComponentModel.Composition;
using DryIocAttributes;
using Grpc.Core;

namespace KillrVideo.Uploads
{
    /// <summary>
    /// Static factory for creating a ServerServiceDefinition for the Uploads Service for use with a Grpc Server.
    /// </summary>
    [Export, AsFactory]
    public static class UploadsServiceFactory
    {
        [Export]
        public static ServerServiceDefinition Create()
        {
            var uploads = new UploadsServiceImpl();
            return UploadsService.BindService(uploads);
        }
    }
}
