using System.Collections.Generic;

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
        public const string UseDseKey = "SuggestedVideos.UseDse";

        /// <summary>
        /// Returns true if DSE Search and Spark are enabled.
        /// </summary>
        internal static bool UseDse(IDictionary<string, string> config)
        {
            string useDse;
            if (config.TryGetValue(UseDseKey, out useDse) == false)
                return false;

            return bool.Parse(useDse);
        }
    }
}
