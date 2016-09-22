namespace KillrVideo.Protobuf
{
    /// <summary>
    /// Options needed to start the gRPC server.
    /// </summary>
    public class ListenOptions
    {
        /// <summary>
        /// The IP address to listen on.
        /// </summary>
        public string IP { get; set; }

        /// <summary>
        /// The port number of listen on.
        /// </summary>
        public int Port { get; set; }
    }
}
