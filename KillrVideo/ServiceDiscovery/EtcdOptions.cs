namespace KillrVideo.ServiceDiscovery
{
    /// <summary>
    /// Options needed to create an etcd client.
    /// </summary>
    public class EtcdOptions
    {
        /// <summary>
        /// The IP/host where etcd is listening.
        /// </summary>
        public string IP { get; set; }

        /// <summary>
        /// The port where etcd is listening.
        /// </summary>
        public int Port { get; set; }
    }
}
