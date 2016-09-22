using System;
using Microsoft.Extensions.Configuration;

namespace KillrVideo.Configuration
{
    /// <summary>
    /// Configuration source for reading parameters from a .env file.
    /// </summary>
    public class EnvironmentFileSource : IConfigurationSource
    {
        public string FilePath { get; }

        public EnvironmentFileSource(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(filePath));

            FilePath = filePath;
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new EnvironmentFileProvider(FilePath);
        }
    }
}