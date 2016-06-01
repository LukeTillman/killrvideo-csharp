using System.ComponentModel.Composition;
using DryIocAttributes;
using KillrVideo.Host.Config;
using KillrVideo.MessageBus.Transport;

namespace KillrVideo.Configuration
{
    /// <summary>
    /// Configuration for the application.
    /// </summary>
    [Export, AsFactory]
    public static class AppConfigurationFactory
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
    }
}
