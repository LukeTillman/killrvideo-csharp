namespace KillrVideo.Utils.Configuration
{
    /// <summary>
    /// Component that can provide configuration information for the current running environment.
    /// </summary>
    public interface IGetEnvironmentConfiguration
    {
        /// <summary>
        /// The name of the currently running application.
        /// </summary>
        string AppName { get; }

        /// <summary>
        /// Gets a unique Id for the running app.
        /// </summary>
        string UniqueInstanceId { get; }

        /// <summary>
        /// Gets a configuration setting's value for the given key.
        /// </summary>
        string GetSetting(string key);
    }
}
