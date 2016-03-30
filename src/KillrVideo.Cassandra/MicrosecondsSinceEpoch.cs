using System;

namespace KillrVideo.Cassandra
{
    /// <summary>
    /// Helper class for converting between DateTimeOffset and microseconds since Epoch which is used for Cassandra write timestamps.
    /// </summary>
    public static class MicrosecondsSinceEpoch
    {
        private static readonly DateTimeOffset Epoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
        private const long TicksPerMicrosecond = 10;

        /// <summary>
        /// Returns the number of microseconds since Epoch of the current UTC date and time.
        /// </summary>
        public static long UtcNow
        {
            get { return FromDateTimeOffset(DateTimeOffset.UtcNow); }
        }

        /// <summary>
        /// Converts the microseconds since Epoch time provided to a DateTimeOffset.
        /// </summary>
        public static DateTimeOffset ToDateTimeOffset(long microsecondsSinceEpoch)
        {
            return Epoch.AddTicks(microsecondsSinceEpoch*TicksPerMicrosecond);
        }

        /// <summary>
        /// Converts the DateTimeOffset provided to the number of microseconds since Epoch.
        /// </summary>
        public static long FromDateTimeOffset(DateTimeOffset dateTimeOffset)
        {
            return dateTimeOffset.Subtract(Epoch).Ticks/TicksPerMicrosecond;
        }

        /// <summary>
        /// Converts the DateTimeOffset provided to the number of microseconds since Epoch.
        /// </summary>
        public static long ToMicrosecondsSinceEpoch(this DateTimeOffset dateTimeOffset)
        {
            return FromDateTimeOffset(dateTimeOffset);
        }
    }
}
