using System.ComponentModel.Composition;
using Cassandra;
using DryIocAttributes;
using Grpc.Core;
using KillrVideo.MessageBus;

namespace KillrVideo.Ratings
{
    /// <summary>
    /// Static factory for creating a ServerServiceDefinition for the Ratings Service for use with a Grpc Server.
    /// </summary>
    [Export, AsFactory]
    public class RatingsServiceFactory
    {
        [Export]
        public static ServerServiceDefinition Create(ISession cassandra, IBus bus)
        {
            var ratingsService = new RatingsServiceImpl(cassandra, bus);
            return RatingsService.BindService(ratingsService);
        }
    }
}
