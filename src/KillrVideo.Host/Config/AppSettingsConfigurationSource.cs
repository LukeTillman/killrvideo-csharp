using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace KillrVideo.Host.Config
{
    /// <summary>
    /// Configuration values from the host's AppSettings.
    /// </summary>
    public class AppSettingsConfigurationSource : IHostConfigurationSource
    {
        private readonly Lazy<IDictionary<string, string>> _configs;
        
        public AppSettingsConfigurationSource()
        {
            _configs = new Lazy<IDictionary<string,string>>(GetAppSettings);
        }

        /// <summary>
        /// Gets a configuration value. Value could be null/empty.
        /// </summary>
        public string GetConfigurationValue(string key)
        {
            string val;
            return _configs.Value.TryGetValue(key, out val) ? val : null;
        }

        private static IDictionary<string, string> GetAppSettings()
        {
            return ConfigurationManager.AppSettings.AllKeys
                .ToDictionary(key => key, key => ConfigurationManager.AppSettings.Get(key), StringComparer.OrdinalIgnoreCase);
        }
    }
}
