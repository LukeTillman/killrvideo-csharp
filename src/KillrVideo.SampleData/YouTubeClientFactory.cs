using System.ComponentModel.Composition;
using DryIocAttributes;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using KillrVideo.Host.Config;

namespace KillrVideo.SampleData
{
    /// <summary>
    /// Static factory for creating YouTube API clients based on the host configuration.
    /// </summary>
    [Export, AsFactory]
    public static class YouTubeClientFactory
    {
        /// <summary>
        /// The key in the Host configuration used to find the YouTube API key.
        /// </summary>
        public const string YouTubeApiConfigKey = "YouTubeApiKey";

        [Export]
        public static YouTubeService Create(IHostConfiguration hostConfig)
        {
            string apiKey = hostConfig.GetConfigurationValue(YouTubeApiConfigKey);
            return new YouTubeService(new BaseClientService.Initializer
            {
                ApiKey = apiKey,
                ApplicationName = "KillrVideo.SampleData"
            });
        }
    }
}
