namespace KillrVideo.Host.Config
{
    /// <summary>
    /// A source for host configuration.
    /// </summary>
    public interface IHostConfigurationSource
    {
        /// <summary>
        /// Gets the configuration value for the key specified and returns null if not found.
        /// </summary>
        string GetConfigurationValue(string key);
    }
}