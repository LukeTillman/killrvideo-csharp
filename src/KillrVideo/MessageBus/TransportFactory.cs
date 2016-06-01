using System.ComponentModel.Composition;
using DryIocAttributes;
using KillrVideo.MessageBus.Transport;

namespace KillrVideo.MessageBus
{
    /// <summary>
    /// Static factory for message bus transport.
    /// </summary>
    [Export, AsFactory]
    public static class TransportFactory
    {
        /// <summary>
        /// Use in memory transport since all services will be running in process together.
        /// </summary>
        [Export]
        public static IMessageTransport Transport = InMemoryTransport.Instance;
    }
}
