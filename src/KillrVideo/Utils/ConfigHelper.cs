using System.Configuration;
using Microsoft.WindowsAzure;

namespace KillrVideo.Utils
{
    /// <summary>
    /// Helper class for getting configurations.  Since we can't remove a setting from a Cloud cscfg file (thus causing CloudConfigurationManager
    /// to fallback to the Web.config), this class will treat empty string values as "omitted" and attempt to get the value from the Web.config's
    /// appSettings section.
    /// </summary>
    public static class ConfigHelper
    {
        private const string RequiredNotFoundMessage = "The required configuration setting {0} could not be found in Cloud or App configuration.";

        /// <summary>
        /// Gets the setting specified from CloudConfigurationManager and if the setting is null/empty, tries the ConfigurationManager.AppSettings
        /// collection.  If that's also null/empty, will throw a ConfigurationErrorsException.
        /// </summary>
        /// <param name="key">The key for the setting to retrieve.</param>
        /// <returns>The setting value.</returns>
        public static string GetRequiredSetting(string key)
        {
            string value = GetOptionalSetting(key);
            if (string.IsNullOrEmpty(value))
                throw new ConfigurationErrorsException(string.Format(RequiredNotFoundMessage, key));

            return value;
        }

        /// <summary>
        /// Gets the setting specified from CloudConfigurationManager and if the setting is null/empty, tries the ConfigurationManager.AppSettings
        /// collection.
        /// </summary>
        /// <param name="key">The key for the setting to retrieve.</param>
        /// <returns>The setting value or null/empty string if no value is found.</returns>
        public static string GetOptionalSetting(string key)
        {
            string value = CloudConfigurationManager.GetSetting(key);
            return string.IsNullOrEmpty(value) ? ConfigurationManager.AppSettings[key] : value;
        }
    }
}