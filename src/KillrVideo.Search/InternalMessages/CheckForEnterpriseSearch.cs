using System;
using Nimbus.MessageContracts;

namespace KillrVideo.Search.InternalMessages
{
    /// <summary>
    /// Request that tells the search service to check if DataStax Enterprise search is available.
    /// </summary>
    [Serializable]
    public class CheckForEnterpriseSearch : IBusRequest<CheckForEnterpriseSearch, EnterpriseSearchAvailability>
    {
    }
}
