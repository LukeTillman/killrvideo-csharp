using System;

namespace KillrVideo.Protobuf
{
    /// <summary>
    /// Helper extension methods for dealing with Uuids and TimeUuids.
    /// </summary>
    public static class UuidExtensions
    {
        /// <summary>
        /// Converts a Guid to a Uuid.
        /// </summary>
        public static Uuid ToUuid(this Guid value)
        {
            return new Uuid { Value = value.ToString() };
        }

        /// <summary>
        /// Converts a Guid to a TimeUuid.
        /// </summary>
        public static TimeUuid ToTimeUuid(this Guid value)
        {
            return new TimeUuid { Value = value.ToString() };
        }
    }
}
