using System.ComponentModel.Composition;
using DryIocAttributes;
using KillrVideo.Host.Config;

namespace KillrVideo.Configuration
{
    /// <summary>
    /// Host configuration for the application.
    /// </summary>
    [Export, AsFactory]
    public static class HostConfigurationFactory
    {
        /// <summary>
        /// Get host settings from the App.config file.
        /// </summary>
        [Export]
        public static readonly IHostConfiguration Config = new AppSettingsConfiguration("KillrVideo", "1");
    }
}
