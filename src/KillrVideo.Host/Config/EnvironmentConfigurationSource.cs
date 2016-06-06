using System;
using System.Text.RegularExpressions;

namespace KillrVideo.Host.Config
{
    /// <summary>
    /// Gets configuration values from environment variables.
    /// </summary>
    public class EnvironmentConfigurationSource : IHostConfigurationSource
    {
        private static readonly Regex MatchCaps = new Regex("[ABCDEFGHIJKLMNOPQRSTUVWXYZ]", RegexOptions.Singleline | RegexOptions.Compiled);

        public string GetConfigurationValue(string key)
        {
            key = ConfigKeyToEnvironmentVariableName(key);
            return Environment.GetEnvironmentVariable(key);
        }

        /// <summary>
        /// Utility method to convert a config key to an approriate environment variable name.
        /// </summary>
        private static string ConfigKeyToEnvironmentVariableName(string key)
        {
            key = MatchCaps.Replace(key, match => match.Index == 0 ? match.Value : $"_{match.Value}");
            return $"KILLRVIDEO_{key.ToUpperInvariant()}";
        }
    }
}