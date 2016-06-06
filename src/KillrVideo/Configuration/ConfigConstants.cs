namespace KillrVideo.Configuration
{
    /// <summary>
    /// Config key constants.
    /// </summary>
    public static class ConfigConstants
    {
        /// <summary>
        /// The IP address of this machine that components running in Docker will be able to communicate with.
        /// </summary>
        public const string HostIp = "HostIp";

        /// <summary>
        /// The IP address of the Docker virtual machine where other components will be running.
        /// </summary>
        public const string DockerIp = "DockerIp";

        /// <summary>
        /// Indicates whether to start docker compose when this project starts.
        /// </summary>
        public const string StartDockerCompose = "StartDockerCompose";

        /// <summary>
        /// Indicates whether to stop docker compose when this project is gracefully shutdown.
        /// </summary>
        public const string StopDockerCompose = "StopDockerCompose";
    }
}
