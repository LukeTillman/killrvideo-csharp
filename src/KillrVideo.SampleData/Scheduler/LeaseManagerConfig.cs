namespace KillrVideo.SampleData.Scheduler
{
    /// <summary>
    /// Some configuration needed by the LeaseManager component.
    /// </summary>
    public class LeaseManagerConfig
    {
        /// <summary>
        /// The name of the lease the manager will be responsible for.
        /// </summary>
        public string LeaseName { get; set; }

        /// <summary>
        /// A unique Id for this node in the cluster of worker machines running the LeaseManager component.
        /// </summary>
        public string UniqueId { get; set; }
    }
}
