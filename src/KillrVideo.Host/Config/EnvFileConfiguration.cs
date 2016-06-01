using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace KillrVideo.Host.Config
{
    /// <summary>
    /// Gets configuration from an .env file of key-value pairs.
    /// </summary>
    public class EnvFileConfiguration : IHostConfiguration
    {
        private readonly FileInfo _envFile;
        private readonly Lazy<IDictionary<string, string>> _configs;

        /// <summary>
        /// The name of the application.
        /// </summary>
        public string ApplicationName { get; }

        /// <summary>
        /// A unique identifier for this particular running instance of the application.
        /// </summary>
        public string ApplicationInstanceId { get; }

        public EnvFileConfiguration(string applicationName, string applicationInstanceId, FileInfo envFile)
        {
            if (envFile == null) throw new ArgumentNullException(nameof(envFile));
            _envFile = envFile;

            ApplicationName = applicationName;
            ApplicationInstanceId = applicationInstanceId;
            _configs = new Lazy<IDictionary<string,string>>(ReadConfigFile);
        }

        public string GetRequiredConfigurationValue(string key)
        {
            string val;
            if (_configs.Value.TryGetValue(key, out val) == false || string.IsNullOrWhiteSpace(val))
                throw new InvalidOperationException($"A value for {key} was not found in {_envFile.FullName}");

            return val;
        }

        public string GetConfigurationValue(string key)
        {
            string val;
            _configs.Value.TryGetValue(key, out val);
            return val;
        }

        private IDictionary<string, string> ReadConfigFile()
        {
            var configs = new Dictionary<string, string>();

            using (FileStream readStream = _envFile.OpenRead())
            using (var reader = new StreamReader(readStream, Encoding.UTF8))
            {
                while (reader.EndOfStream == false)
                {
                    string config = reader.ReadLine();

                    // Skip empty lines
                    if (string.IsNullOrWhiteSpace(config))
                        continue;

                    // Skip comments
                    if (config.StartsWith("#"))
                        continue;

                    // Split config keys and values and add to dictionary
                    string[] configParts = config.Split('=');
                    if (configParts.Length != 2)
                        throw new InvalidOperationException($"Bad value {config} in {_envFile.FullName}");

                    configs.Add(configParts[0], configParts[1]);
                }
            }

            return configs;
        }
    }
}
