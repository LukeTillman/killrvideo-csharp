using System.Net;
using System.Threading.Tasks;

namespace KillrVideo.Protobuf.ServiceDiscovery
{
    /// <summary>
    /// Component for locating services in the catalog.
    /// </summary>
    public interface IFindGrpcServices
    {
        /// <summary>
        /// Finds a service in the catalog. Returns null if the service cannot be located.
        /// </summary>
        Task<IPEndPoint> Find(string serviceName);
    }
}