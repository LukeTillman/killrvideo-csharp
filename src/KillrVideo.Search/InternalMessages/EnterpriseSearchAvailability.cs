using System;
using Nimbus.MessageContracts;

namespace KillrVideo.Search.InternalMessages
{
    /// <summary>
    /// Response that indicates whether DataStax Enterprise search is available.
    /// </summary>
    [Serializable]
    public class EnterpriseSearchAvailability : IBusResponse
    {
        /// <summary>
        /// True if DataStax search is available, otherwise false.
        /// </summary>
        public bool IsAvailable { get; set; }
    }
}