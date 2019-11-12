namespace KillrVideo.Listeners
{
    /// <summary>
    /// Options that represent the broadcast address for gRPC services running on the host.
    /// </summary>
    public class BroadcastOptions
    {
        /// <summary>
        /// The IP address to broadcast.
        /// </summary>
        public string IP { get; set; }

        /// <summary>
        /// The port to broadcast.
        /// </summary>
        public int Port { get; set; }
    }
}
