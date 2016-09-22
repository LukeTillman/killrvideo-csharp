using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace KillrVideo.Configuration
{
    /// <summary>
    /// Configuration provider that reads KEY=VALUE pairs from a file.
    /// </summary>
    public class EnvironmentFileProvider : ConfigurationProvider
    {
        private readonly FileInfo _fileInfo;

        public EnvironmentFileProvider(string filePath)
        {
            _fileInfo = new FileInfo(filePath);
        }

        public override void Load()
        {
            Data = new Dictionary<string, string>();

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

                    Data[configParts[0]] = configParts[1];
                }
            }
        }
    }
}
