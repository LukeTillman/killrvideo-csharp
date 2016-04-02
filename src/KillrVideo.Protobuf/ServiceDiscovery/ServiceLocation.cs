using System;

namespace KillrVideo.Protobuf.ServiceDiscovery
{
    /// <summary>
    /// Service information returned from service discovery implementations.
    /// </summary>
    public class ServiceLocation : IEquatable<ServiceLocation>
    {
        public string Host { get; }
        public int Port { get; }

        public ServiceLocation(string host, int port)
        {
            Host = host;
            Port = port;
        }

        public bool Equals(ServiceLocation other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Host, other.Host) && Port == other.Port;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ServiceLocation) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Host.GetHashCode()*397) ^ Port;
            }
        }

        public static bool operator ==(ServiceLocation left, ServiceLocation right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ServiceLocation left, ServiceLocation right)
        {
            return !Equals(left, right);
        }
    }
}
