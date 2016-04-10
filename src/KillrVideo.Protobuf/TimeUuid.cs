using System;

namespace KillrVideo.Protobuf
{
    public sealed partial class TimeUuid
    {
        /// <summary>
        /// Converts the TimeUuid string value to a Guid.
        /// </summary>
        public Guid ToGuid()
        {
            return Guid.Parse(Value);
        }

        /// <summary>
        /// Converts the TimeUuid string value to a nullable Guid.
        /// </summary>
        public Guid? ToNullableGuid()
        {
            if (string.IsNullOrEmpty(Value))
                return null;

            return ToGuid();
        }
    }
}
