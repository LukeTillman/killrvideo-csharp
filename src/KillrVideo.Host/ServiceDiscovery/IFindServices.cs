using System.Collections.Generic;
using System.Threading.Tasks;

namespace KillrVideo.Host.ServiceDiscovery
{
    /// <summary>
    /// Component responsible for finding service locations.
    /// </summary>
    public interface IFindServices
    {
        /// <summary>
        /// Looks up a service by name. Returns a collection of strings with the format of "hostname:port".
        /// </summary>
        Task<IEnumerable<string>> LookupServiceAsync(string serviceName);
    }
}
