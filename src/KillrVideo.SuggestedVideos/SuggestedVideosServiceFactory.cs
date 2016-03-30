using System.ComponentModel.Composition;
using Cassandra;
using DryIocAttributes;
using Grpc.Core;

namespace KillrVideo.SuggestedVideos
{
    /// <summary>
    /// Static factory for creating a ServerServiceDefinition for the Suggested Videos Service for use with a Grpc Server.
    /// </summary>
    [Export, AsFactory]
    public static class SuggestedVideosServiceFactory
    {
        [Export]
        public static ServerServiceDefinition Create(ISession cassandra)
        {
            // TODO: Which implementation based on config or detect C* cluster version?
            var suggestionsService = new SuggestVideosByTag(cassandra);
            return SuggestedVideoService.BindService(suggestionsService);
        }
    }
}
