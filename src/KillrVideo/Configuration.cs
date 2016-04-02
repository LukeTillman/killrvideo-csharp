using System.ComponentModel.Composition;
using DryIocAttributes;
using KillrVideo.Host.Config;
using KillrVideo.MessageBus.Transport;
using KillrVideo.Protobuf.ServiceDiscovery;

namespace KillrVideo
{
    /// <summary>
    /// Configuration for the application.
    /// </summary>
    [Export, AsFactory]
    public static class Configuration
    {
        /// <summary>
        /// Get host settings from the App.config file.
        /// </summary>
        [Export]
        public static readonly IHostConfiguration Config = new AppSettingsConfiguration("KillrVideo", "1");

        /// <summary>
        /// Use in memory transport since all services will be running in process together.
        /// </summary>
        [Export]
        public static IMessageTransport Transport = InMemoryTransport.Instance;

        /// <summary>
        /// Use local service discovery since all services are running in process together on the same IP.
        /// </summary>
        [Export]
        public static readonly IFindGrpcServices ServiceDiscovery = new LocalServiceDiscovery(Config);
    }
}
