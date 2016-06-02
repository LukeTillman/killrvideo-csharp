using System;
using System.IO;
using KillrVideo.Host.Config;

namespace KillrVideo.Configuration
{
    public class TemporaryHybridConfiguration : IHostConfiguration
    {
        private readonly EnvFileConfiguration _config1;
        private readonly AppSettingsConfiguration _config2;

        public string ApplicationName { get; }
        public string ApplicationInstanceId { get; }

        public TemporaryHybridConfiguration(string applicationName, string applicationInstanceId)
        {
            ApplicationName = applicationName;
            ApplicationInstanceId = applicationInstanceId;

            var envFile = new FileInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".\\.env"));
            _config1 = new EnvFileConfiguration(applicationName, applicationInstanceId, envFile);
            _config2 = new AppSettingsConfiguration(applicationName, applicationInstanceId);
        }

        public string GetRequiredConfigurationValue(string key)
        {
            string val = GetConfigurationValue(key);
            if (string.IsNullOrEmpty(val))
                throw new InvalidOperationException($"Could not find value for config key {key}");
            return val;
        }

        public string GetConfigurationValue(string key)
        {
            string val = _config1.GetConfigurationValue(key);
            if (string.IsNullOrEmpty(val) == false)
                return val;

            return _config2.GetConfigurationValue(key);
        }
    }
}
