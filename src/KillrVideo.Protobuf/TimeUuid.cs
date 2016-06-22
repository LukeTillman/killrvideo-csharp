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
    }
}
