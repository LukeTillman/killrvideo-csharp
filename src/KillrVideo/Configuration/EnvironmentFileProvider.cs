using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace KillrVideo.Configuration
{
    /// <summary>
    /// Configuration provider that reads KEY=VALUE pairs from a .env file and maps those key value pairs to
    /// configuration keys.
    /// </summary>
    public class EnvironmentFileProvider : ConfigurationProvider
    {
        private readonly IDictionary<string, string> _mappings;
        private readonly FileInfo _fileInfo;

        public EnvironmentFileProvider(IDictionary<string, string> mappings)
        {
            _mappings = mappings;
            string exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            _fileInfo = new FileInfo(Path.Combine(exePath, @".\.env"));
        }

        public override void Load()
        {
            // Load the .env file contents
            var fileContents = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using (FileStream readStream = _fileInfo.OpenRead())
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
                        throw new InvalidOperationException($"Bad value {config} in {_fileInfo.FullName}");

                    fileContents.Add(configParts[0], configParts[1]);
                }
            }

            // Now use the mappings to pull data from the file contents
            foreach (KeyValuePair<string, string> mapping in _mappings)
            {
                string valFromFile;
                if (fileContents.TryGetValue(mapping.Value, out valFromFile) == false)
                    continue;

                Data[mapping.Key] = valFromFile;
            }
        }
    }
}
