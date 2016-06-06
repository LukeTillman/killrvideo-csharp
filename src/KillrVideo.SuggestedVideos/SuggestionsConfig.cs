using KillrVideo.Host;
using KillrVideo.Host.Config;

namespace KillrVideo.SuggestedVideos
{
    /// <summary>
    /// Configuration helper for the Videos Suggestions service.
    /// </summary>
    public static class SuggestionsConfig
    {
        /// <summary>
        /// The configuration key that determines whether to use DSE Search + Spark for the Suggested Videos service.
        /// </summary>
        public const string UseDseKey = "UseDse";

        /// <summary>
        /// Returns true if DSE Search and Spark are enabled.
        /// </summary>
        internal static bool UseDse(IHostConfiguration config)
        {
            string useDse = config.GetConfigurationValueOrDefault(UseDseKey, bool.FalseString);
            return bool.Parse(useDse);
        }
    }
}
