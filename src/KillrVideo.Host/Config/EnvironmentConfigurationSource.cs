using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace KillrVideo.Host.Config
{
    /// <summary>
    /// Gets configuration values from command line args and environment variables.
    /// </summary>
    public class EnvironmentConfigurationSource : IHostConfigurationSource
    {
        private static readonly Regex MatchCaps = new Regex("[ABCDEFGHIJKLMNOPQRSTUVWXYZ]", RegexOptions.Singleline | RegexOptions.Compiled);
        private readonly Lazy<IDictionary<string, string>> _commandLineArgs;

        public EnvironmentConfigurationSource()
        {
            _commandLineArgs = new Lazy<IDictionary<string,string>>(ParseCommandLineArgs);
        }

        public string GetConfigurationValue(string key)
        {
            // See if command line had it
            string val;
            if (_commandLineArgs.Value.TryGetValue(key, out val))
                return val;

            // See if environment variables have it
            key = ConfigKeyToEnvironmentVariableName(key);
            return Environment.GetEnvironmentVariable(key);
        }

        private static IDictionary<string, string> ParseCommandLineArgs()
        {
            var results = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Get command line args but skip the first one which will be the process/executable name
            string[] args = Environment.GetCommandLineArgs().Skip(1).ToArray();

            string argName = null;
            foreach (string arg in args)
            {
                // Do we have an argument?
                if (arg.StartsWith("-"))
                {
                    // If we currently have an argName, assume it was a switch and just add TrueString as the value
                    if (argName != null)
                        results.Add(argName, bool.TrueString);

                    argName = arg.TrimStart('-');
                    continue;
                }

                // Do we have an argument that doesn't have a previous arg name?
                if (argName == null)
                    throw new InvalidOperationException($"Unknown command line argument {arg}");

                // Add argument value under previously parsed arg name
                results.Add(argName, arg);
                argName = null;
            }

            return results;
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