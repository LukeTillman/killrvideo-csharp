using System.Collections.Generic;

namespace KillrVideo.Search
{
    /// <summary>
    /// Configuration helper for configuring the search service.
    /// </summary>
    public static class SearchConfig
    {
        /// <summary>
        /// The configuration key that determines whether to use DSE search for the Search Service.
        /// </summary>
        public const string UseDseKey = "Search.UseDse";

        /// <summary>
        /// Returns true if using DSE Search is enabled.
        /// </summary>
        internal static bool UseDseSearch(IDictionary<string, string> config)
        {
            string useDse;
            if (config.TryGetValue(UseDseKey, out useDse) == false)
                return false;

            return bool.Parse(useDse);
        }
    }
}
