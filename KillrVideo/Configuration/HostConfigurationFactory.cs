﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using DryIocAttributes;
using KillrVideo.Host;
using KillrVideo.Listeners;
using KillrVideo.Protobuf;
using KillrVideo.Search;
using KillrVideo.ServiceDiscovery;
using KillrVideo.SuggestedVideos;
using KillrVideo.UserManagement;
using Microsoft.Extensions.Configuration;

namespace KillrVideo.Configuration
{
    /// <summary>
    /// String tokens for graph element lables and property keys.
    /// </summary>
    public static class ConfigKeys {

        public const String DseKeySpace         = "DseKeySpace";
        public const String DseGraphName        = "DseGraphName";

        public const String DseGraphReadTimeout = "DseGraphReadTimeout";

        public const String DseUsername         = "DseUsername";
        public const String DsePassword         = "DsePassword";
        public const String DseEnableSsl        = "DseEnableSSL";
        public const String DseSslCertPath      = "DseSslCertPath";
        public const String DseSslCertPassword  = "DseSslCertPassword";

        public const String MaxRetry            = "AttemptsBeforeLoggingErrors";
        public const String RetryDelay          = "RetryDelay";
        public const String EtcdCassandraKey    = "cassandra";
    }

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
                // Allow fallback to configuration from the Docker .env file
                .Add(new EnvironmentFileSource(new Dictionary<string, string>
                {
                    // The IP address for etcd to do service discovery
                    { "Etcd:IP", "KILLRVIDEO_DOCKER_IP" },
                    // The IP address to broadcast for gRPC services (i.e. register with service discovery)
                    { "Broadcast:IP", "KILLRVIDEO_HOST_IP" }
                }))
                // Add the configuration defaults
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    // Externalization of used KeySpace
                    { ConfigKeys.DseKeySpace, "killrvideo" },
                    // Externalization of used KeySpace
                    { ConfigKeys.DseGraphName, "killrvideo_video_recommendations" },
                    // Externalization of timeout
                    { ConfigKeys.DseGraphReadTimeout, "30000" },
                    // UserName
                    { ConfigKeys.DseUsername, "" },
                    // Password
                    { ConfigKeys.DsePassword, "" },
                    // Enable SSL
                    { ConfigKeys.DseEnableSsl, "false" },
                    // SSL Certificate Path
                    { ConfigKeys.DseSslCertPath, "C:\\TMP\\sample.cert" },
                    // SSL Certificate Password
                    { ConfigKeys.DseSslCertPassword, "change_me" },
                    // Number of tries before leaving
                    { ConfigKeys.MaxRetry, "6" },
                    // Number of tries before leaving
                    { ConfigKeys.RetryDelay, "10000" },

                    // The default logging output level
                    { "LoggingLevel", "verbose" },
                    // Whether to use DSE implementations of services
                    { "DseEnabled", "true" },
                    // The name of this application
                    { "AppName", "killrvideo-csharp" },
                    // A unique instance number for this application
                    { "AppInstance", "1" },
                    // The port for etcd to do service discovery
                    { "Etcd:Port", "2379" },
                    // The IP address for gRPC services to listen on
                    { "Listen:IP", "0.0.0.0" },
                    // The Port for gRPC services to listen on
                    { "Listen:Port", "50101" },
                    // The Port to broadcast for gRPC services (i.e. register with service discovery)
                    { "Broadcast:Port", "50101" },
                    // Whether to use any LINQ implementations of services
                    { "LinqEnabled", "false" }
                })
                // Allow configuration via environment variables
                .AddEnvironmentVariables("KILLRVIDEO_")
                // Allow configuration via commandline parameters
                .AddCommandLine(commandLineArgs.Args)
                .Build();
        }

        [Export]
        public static HostOptions GetHostOptions(IConfiguration configuration)
        {
            return GetOptions<HostOptions>(configuration);
        }

        [Export]
        public static ListenOptions GetListenOptions(IConfiguration configuration)
        {
            var options = new ListenOptions();
            configuration.GetSection("Listen").Bind(options);
            
            return options;
        }

        [Export]
        public static SearchOptions GetSearchOptions(IConfiguration configuration)
        {
            return GetOptions<SearchOptions>(configuration);
        }

        [Export]
        public static SuggestionsOptions GetSuggestionsOptions(IConfiguration configuration)
        {
            return GetOptions<SuggestionsOptions>(configuration);
        }

        [Export]
        public static UserManagementOptions GetUserManagementOptions(IConfiguration configuration)
        {
            return GetOptions<UserManagementOptions>(configuration);
        }

        [Export]
        public static BroadcastOptions GetBroadcastOptions(IConfiguration configuration)
        {
            var options = new BroadcastOptions();
            configuration.GetSection("Broadcast").Bind(options);
            return options;
        }

        [Export]
        public static EtcdOptions GetEtcdOptions(IConfiguration configuration)
        {
            var options = new EtcdOptions();
            configuration.GetSection("Etcd").Bind(options);
            return options;
        }

        private static T GetOptions<T>(IConfiguration configuration)
            where T : new()
        {
            var options = new T();
            configuration.Bind(options);
            return options;
        }
    }
}