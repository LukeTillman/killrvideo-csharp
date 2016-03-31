using System;
using System.ComponentModel.Composition;
using DryIocAttributes;
using Grpc.Core;
using KillrVideo.Cassandra;

namespace KillrVideo.Search
{
    /// <summary>
    /// Factory for creating the appropriate Search Service definition based on whether DSE is available or not.
    /// </summary>
    [Export, AsFactory]
    public class SearchServiceFactory
    {
        private readonly Func<DataStaxEnterpriseSearch> _dseSearch;
        private readonly Func<SearchVideosByTag> _byTag;

        public SearchServiceFactory(Func<DataStaxEnterpriseSearch> dseSearch, Func<SearchVideosByTag> byTag)
        {
            if (dseSearch == null) throw new ArgumentNullException(nameof(dseSearch));
            if (byTag == null) throw new ArgumentNullException(nameof(byTag));
            _dseSearch = dseSearch;
            _byTag = byTag;
        }

        [Export]
        public ServerServiceDefinition Create(DataStaxEnterpriseEnvironmentState currentState)
        {
            var searchImpl = currentState.HasFlag(DataStaxEnterpriseEnvironmentState.SearchEnabled)
                                 ? (SearchService.ISearchService) _dseSearch()
                                 : _byTag();
            return SearchService.BindService(searchImpl);
        }
    }
}
