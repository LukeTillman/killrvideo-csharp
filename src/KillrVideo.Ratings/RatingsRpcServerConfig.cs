using Cassandra;
using KillrVideo.MessageBus.Transport;

namespace KillrVideo.Ratings
{
    /// <summary>
    /// Configuration needed to start the Ratings Service RPC server.
    /// </summary>
    public class RatingsRpcServerConfig
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public ISession Cassandra { get; set; }
        public IMessageTransport BusTransport { get; set; }
    }
}