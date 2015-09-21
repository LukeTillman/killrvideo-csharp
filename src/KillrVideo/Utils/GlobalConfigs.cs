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

        /// <summary>
        /// Whether or not sample data entry is enabled for the web site.
        /// </summary>
        public static readonly bool SampleDataEntryEnabled;

        /// <summary>
        /// Whether or not to show the personalized recommendations UI.
        /// </summary>
        public static readonly bool RecommendationsEnabled;

        static GlobalConfigs()
        {
            // See if analytics are enabled in the configuration file
            const string analyticsEnabledKey = "AnalyticsEnabled";
            AnalyticsEnabled = GetBoolConfigValue(analyticsEnabledKey);

            // See if sample data entry is enabled in the configuration file
            const string sampleDataEntryEnabledKey = "SampleDataEntryEnabled";
            SampleDataEntryEnabled = GetBoolConfigValue(sampleDataEntryEnabledKey);

            // See if we should show the personalized recommendations UI
            const string recommendationsEnabledKey = "RecommendationsEnabled";
            RecommendationsEnabled = GetBoolConfigValue(recommendationsEnabledKey);
        }

        private static bool GetBoolConfigValue(string key)
        {
            bool value;
            if (bool.TryParse(CloudConfigurationManager.GetSetting(key), out value) == false)
                value = false;

            return value;
        }
    }
}