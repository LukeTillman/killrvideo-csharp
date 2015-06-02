using System.Threading.Tasks;
using KillrVideo.Search.InternalMessages;
using Nimbus.Handlers;

namespace KillrVideo.Search.Worker.Handlers
{
    /// <summary>
    /// Handler that sets up DataStax Enterprise search (if available) and responds with a status indicating if
    /// enterprise search is available.
    /// </summary>
    public class SetupDataStaxSearch : IHandleRequest<CheckForEnterpriseSearch, EnterpriseSearchAvailability>
    {
        public Task<EnterpriseSearchAvailability> Handle(CheckForEnterpriseSearch request)
        {
            // TODO: Implement bootstrapping and check
            return Task.FromResult(new EnterpriseSearchAvailability { IsAvailable = false });
        }
    }
}
