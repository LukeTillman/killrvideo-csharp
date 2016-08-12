using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Text;
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
        /// Get host settings from a combination of environment variables and app settings.
        /// </summary>
        [Export]
        public static IHostConfiguration GetHostConfiguration()
        {
            ReadEnvironmentFile();
            var sources = new IHostConfigurationSource[]
            {
                new EnvironmentConfigurationSource()
            };
            return new HostConfiguration(sources, "KillrVideo", "1");
        }

        private static void ReadEnvironmentFile()
        {
            // We should have a .env file, so read values from that and set Environment variables appropriately
            var envFile = new FileInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".\\.env"));
            using (FileStream readStream = envFile.OpenRead())
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
                        throw new InvalidOperationException($"Bad value {config} in {envFile.FullName}");

                    Environment.SetEnvironmentVariable(configParts[0], configParts[1]);
                }
            }
        }
    }
}
