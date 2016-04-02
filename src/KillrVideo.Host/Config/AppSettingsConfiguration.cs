using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;

namespace KillrVideo.Host.Config
{
    /// <summary>
    /// Configuration values from the host's AppSettings.
    /// </summary>
    public class AppSettingsConfiguration : IHostConfiguration
    {
        private readonly IDictionary<string, string> _config;

        /// <summary>
        /// The name of the application.
        /// </summary>
        public string ApplicationName { get; }

        public AppSettingsConfiguration(string applicationName)
        {
            ApplicationName = applicationName;
            _config = ConfigurationManager.AppSettings.AllKeys.ToDictionary(key => key, key => ConfigurationManager.AppSettings.Get(key));
        }

        /// <summary>
        /// Gets a required configuration value and throws an InvalidOperationException if the value is not present or is null/empty.
        /// </summary>
        public string GetRequiredConfigurationValue(string key)
        {
            string val;
            if (_config.TryGetValue(key, out val) == false || string.IsNullOrWhiteSpace(val))
                throw new InvalidOperationException($"You must specify a value for {key} in your .config file");

            return val;
        }

        /// <summary>
        /// Gets a configuration value. Value could be null/empty.
        /// </summary>
        public string GetConfigurationValue(string key)
        {
            string val;
            _config.TryGetValue(key, out val);
            return val;
        }
    }
}
