using System.Threading.Tasks;
using Google.Protobuf.Reflection;
using Grpc.Core;

namespace KillrVideo.Protobuf.Clients
{
    /// <summary>
    /// Component responsible for getting Channels for a given service.
    /// </summary>
    public interface IChannelFactory
    {
        /// <summary>
        /// Gets a channel to the given service. Will throw if the service cannot be found.
        /// </summary>
        Task<Channel> GetChannelAsync(ServiceDescriptor service);
    }
}