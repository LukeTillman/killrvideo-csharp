using System;

namespace KillrVideo.Host.ServiceDiscovery
{
    /// <summary>
    /// Exception thrown when a service could not be found.
    /// </summary>
    public class ServiceNotFoundException : Exception
    {
        public ServiceNotFoundException(string serviceName)
            : base($"Could not find service {serviceName}")
        {
        }
    }
}