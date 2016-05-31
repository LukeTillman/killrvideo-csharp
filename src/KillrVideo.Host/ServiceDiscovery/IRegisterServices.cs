using System.Threading.Tasks;

namespace KillrVideo.Host.ServiceDiscovery
{
    /// <summary>
    /// Registers a service with service discovery.
    /// </summary>
    public interface IRegisterServices
    {
        /// <summary>
        /// Sets the location for a service.
        /// </summary>
        Task SetServiceLocation(string serviceName, string location);
    }
}