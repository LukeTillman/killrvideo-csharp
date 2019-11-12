using System;

namespace KillrVideo.VideoCatalog
{
    /// <summary>
    /// Helper extension methods.
    /// </summary>
    public static class HelperExtensions
    {
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
