using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace KillrVideo.Configuration
{
    /// <summary>
    /// Configuration source for reading parameters from a .env file.
    /// </summary>
    public class EnvironmentFileSource : IConfigurationSource
    {
        public IDictionary<string, string> Mappings { get; set; }

        public EnvironmentFileSource(IDictionary<string, string> mappings)
        {
            Mappings = mappings;
            if (mappings == null) throw new ArgumentNullException(nameof(mappings));
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new EnvironmentFileProvider(Mappings);
        }
    }
}