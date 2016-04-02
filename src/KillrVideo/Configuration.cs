using System.ComponentModel.Composition;
using DryIocAttributes;
using KillrVideo.Host.Config;
using KillrVideo.MessageBus.Transport;

namespace KillrVideo
{
    /// <summary>
    /// Configuration for the application.
    /// </summary>
    [Export, AsFactory]
    public static class Configuration
    {
        [Export]
        public static readonly IHostConfiguration Config = new AppSettingsConfiguration("KillrVideo");

        [Export]
        public static IMessageTransport Transport = InMemoryTransport.Instance;
    }
}
