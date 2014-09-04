using Microsoft.WindowsAzure;

namespace KillrVideo.Utils
{
    /// <summary>
    /// Some global configuration values.
    /// </summary>
    public static class GlobalConfigs
    {
        /// <summary>
        /// Whether or not Google Analytics are enabled for the web site.
        /// </summary>
        public static readonly bool AnalyticsEnabled;

        static GlobalConfigs()
        {
            // See if analytics are enabled in the configuration file
            const string analyticsEnabledKey = "AnalyticsEnabled";
            bool analyticsEnabled;
            if (bool.TryParse(CloudConfigurationManager.GetSetting(analyticsEnabledKey), out analyticsEnabled) == false)
                analyticsEnabled = false;

            AnalyticsEnabled = analyticsEnabled;
        }
    }
}