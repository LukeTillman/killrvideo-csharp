using System.ComponentModel.Composition;
using Cassandra;
using DryIocAttributes;
using Grpc.Core;

namespace KillrVideo.Statistics
{
    /// <summary>
    /// Static factory for creating a ServerServiceDefinition for the Statistics Service for use with a Grpc Server.
    /// </summary>
    [Export, AsFactory]
    public static class StatisticsServiceFactory
    {
        [Export]
        public static ServerServiceDefinition Create(ISession cassandra)
        {
            var statsService = new StatisticsServiceImpl(cassandra);
            return StatisticsService.BindService(statsService);
        }
    }
}
