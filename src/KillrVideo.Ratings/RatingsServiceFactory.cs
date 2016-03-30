using System.ComponentModel.Composition;
using Cassandra;
using DryIocAttributes;
using Grpc.Core;
using KillrVideo.Cassandra;
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
        public static ServerServiceDefinition Create(ISession cassandra, PreparedStatementCache statementCache, IBus bus)
        {
            var ratingsService = new RatingsServiceImpl(cassandra, statementCache, bus);
            return RatingsService.BindService(ratingsService);
        }
    }
}
