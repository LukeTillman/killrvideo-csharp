using System;

namespace KillrVideo.Host.Config
{
    /// <summary>
    /// Host configuration implementation that will use all the sources provided (in order) to try
    /// and resolve configuration values.
    /// </summary>
    public class HostConfiguration : IHostConfiguration
    {
        private readonly IHostConfigurationSource[] _sources;

        public string ApplicationName { get; }

        public string ApplicationInstanceId { get; }

        public HostConfiguration(IHostConfigurationSource[] sources, string applicationName, string applicationInstanceId)
        {
            if (sources == null) throw new ArgumentNullException(nameof(sources));
            _sources = sources;
            ApplicationName = applicationName;
            ApplicationInstanceId = applicationInstanceId;
        }

        public string GetRequiredConfigurationValue(string key)
        {
            string val = GetConfigurationValue(key);
            if (val == null)
                throw new InvalidOperationException($"Could not find configuration value for {key}");

            return val;
        }

        public string GetConfigurationValue(string key)
        {
            foreach (IHostConfigurationSource source in _sources)
            {
                string val = source.GetConfigurationValue(key);
                if (val != null) return val;
            }

            return null;
        }
    }
}
