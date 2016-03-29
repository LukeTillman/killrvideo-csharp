using Cassandra;
using KillrVideo.MessageBus.Transport;

namespace KillrVideo.Comments
{
    /// <summary>
    /// The configuration needed to start the CommentsRpcServer.
    /// </summary>
    public class CommentsRpcServerConfig
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public ISession Cassandra { get; set; }
        public IMessageTransport BusTransport { get; set; }
    }
}
