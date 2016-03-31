using System;
using System.ComponentModel.Composition;
using DryIocAttributes;
using Grpc.Core;
using KillrVideo.Cassandra;

namespace KillrVideo.SuggestedVideos
{
    /// <summary>
    /// Static factory for creating a ServerServiceDefinition for the Suggested Videos Service for use with a Grpc Server.
    /// </summary>
    [Export, AsFactory]
    public class SuggestedVideosServiceFactory
    {
        private readonly Func<DataStaxEnterpriseSuggestedVideos> _dseSearch;
        private readonly Func<SuggestVideosByTag> _byTags;

        public SuggestedVideosServiceFactory(Func<DataStaxEnterpriseSuggestedVideos> dseSearch, Func<SuggestVideosByTag> byTags)
        {
            if (dseSearch == null) throw new ArgumentNullException(nameof(dseSearch));
            if (byTags == null) throw new ArgumentNullException(nameof(byTags));
            _dseSearch = dseSearch;
            _byTags = byTags;
        }

        [Export]
        public ServerServiceDefinition Create(DataStaxEnterpriseEnvironmentState currentState)
        {
            var serviceImpl = currentState.HasFlag(DataStaxEnterpriseEnvironmentState.SearchEnabled)
                                  ? (SuggestedVideoService.ISuggestedVideoService) _dseSearch()
                                  : _byTags();
            return SuggestedVideoService.BindService(serviceImpl);
        }
    }
}
