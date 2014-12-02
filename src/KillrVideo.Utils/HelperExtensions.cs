using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KillrVideo.Utils
{
    /// <summary>
    /// Some utility extension methods.
    /// </summary>
    public static class HelperExtensions
    {
        /// <summary>
        /// Converts an IEnumerable&lt;T&gt; to a HashSet&lt;T&gt;.  If the IEnumerable is null, returns an empty HashSet.
        /// </summary>
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> enumerable)
        {
            if (enumerable == null) return new HashSet<T>();
            return new HashSet<T>(enumerable);
        }

        /// <summary>
        /// Truncates a DateTimeOffset to the specified resolution.  Use the TimeSpan.TicksPerXXX constants for
        /// the resolution parameter.  Returns a new DateTimeOffset.
        /// </summary>
        public static DateTimeOffset Truncate(this DateTimeOffset dateTimeOffset, long resolution)
        {
            return new DateTimeOffset(dateTimeOffset.Ticks - (dateTimeOffset.Ticks % resolution), dateTimeOffset.Offset);
        }
    }
}
