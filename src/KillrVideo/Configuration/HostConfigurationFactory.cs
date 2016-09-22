using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using DryIocAttributes;
using KillrVideo.Host;
using KillrVideo.Protobuf;
using Microsoft.Extensions.Configuration;

namespace KillrVideo.Configuration
{
    /// <summary>
    /// Class that contains static factory methods for getting the various options/config objects needed by the application.
    /// </summary>
    [Export, AsFactory]
    public static class HostConfigurationFactory
    {
        [Export]
        public static IConfiguration GetConfigurationRoot(CommandLineArgs commandLineArgs)
        {
            return new ConfigurationBuilder()
                // Add the configuration defaults
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    // The default logging output level
                    { "LoggingLevel", "verbose" },
                    // Whether to use DSE implementations of services
                    { "DseEnabled", "false" },
                    // The name of this application
                    { "AppName", "killrvideo-csharp" },
                    // A unique instance number for this application
                    { "AppInstance", "1" },
                    // The IP address for etcd to do service discovery
                    { "Etcd:IP", "TODO: DOCKERIP" },
                    // The port for etcd to do service discovery
                    { "Etcd:Port", "2379" },
                    // The IP address for gRPC services to listen on
                    { "Listen:IP", "0.0.0.0" },
                    // The Port for gRPC services to listen on
                    { "Listen:Port", "50101" },
                    // The IP address to broadcast for gRPC services (i.e. register with service discovery)
                    { "Broadcast:IP", "TODO: HOSTIP" },
                    // The Port to broadcast for gRPC services (i.e. register with service discovery)
                    { "Broadcast:Port", "50101" }
                })
                // Allow configuration from the Docker .env file
                .Add(new EnvironmentFileSource(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".\\.env")))
                // Allow configuration via environment variables
                .AddEnvironmentVariables("KILLRVIDEO")
                // Allow configuration via commandline parameters
                .AddCommandLine(commandLineArgs.Args)
                .Build();
        }

        [Export]
        public static HostOptions GetHostOptions(IConfiguration configuration)
        {
            var options = new HostOptions();
            configuration.Bind(options);
            return options;
        }

        [Export]
        public static ListenOptions GetListenOptions(IConfiguration configuration)
        {
            var options = new ListenOptions();
            configuration.GetSection("Listen").Bind(options);
            return options;
        }
    }
}
