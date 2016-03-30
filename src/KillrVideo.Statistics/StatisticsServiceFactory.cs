using System.ComponentModel.Composition;
using Cassandra;
using DryIocAttributes;
using Grpc.Core;
using KillrVideo.Cassandra;

namespace KillrVideo.Statistics
{
    /// <summary>
    /// Static factory for creating a ServerServiceDefinition for the Statistics Service for use with a Grpc Server.
    /// </summary>
    [Export, AsFactory]
    public static class StatisticsServiceFactory
    {
        [Export]
        public static ServerServiceDefinition Create(ISession cassandra, PreparedStatementCache statementCache)
        {
            var statsService = new StatisticsServiceImpl(cassandra, statementCache);
            return StatisticsService.BindService(statsService);
        }
    }
}
