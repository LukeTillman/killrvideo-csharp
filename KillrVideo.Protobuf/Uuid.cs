using System;

namespace KillrVideo.Protobuf
{
    public sealed partial class Uuid
    {
        /// <summary>
        /// Converts the Uuid string value to a Guid.
        /// </summary>
        public Guid ToGuid()
        {
            return Guid.Parse(Value);
        }
    }
}
