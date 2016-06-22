using System;
using System.Collections.Generic;
using System.Linq;
using KillrVideo.Host.Config;

namespace KillrVideo.Host
{
    public static class HelperExtensions
    {
        /// <summary>
        /// Gets all inner exceptions on the AggregateException that aren't TaskCanceledExceptions.
        /// </summary>
        public static IEnumerable<Exception> IgnoreTaskCanceled(this AggregateException ae)
        {
            return ae.InnerExceptions.Where(e => !(e is OperationCanceledException));
        }

        /// <summary>
        /// Gets a configuration key's value from the host configuration and if the value is null/empty, returns the default value specified.
        /// </summary>
        public static string GetConfigurationValueOrDefault(this IHostConfiguration config, string configKey,
            string defaultValue)
        {
            string val = config.GetConfigurationValue(configKey);
            return string.IsNullOrEmpty(val) ? defaultValue : val;
        }
    }
}
