using System;
using Google.Protobuf.Reflection;

namespace KillrVideo.Protobuf.Clients
{
    /// <summary>
    /// Exception thrown when a service could not be found.
    /// </summary>
    public class ServiceNotFoundException : Exception
    {
        public ServiceDescriptor Descriptor { get; private set; }

        public ServiceNotFoundException(ServiceDescriptor service)
            : base($"Could not find service {service.FullName}")
        {
            Descriptor = service;
        }
    }
}