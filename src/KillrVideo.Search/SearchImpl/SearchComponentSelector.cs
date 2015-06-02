using System;
using System.Reflection;
using System.Threading.Tasks;
using Castle.Facilities.TypedFactory;
using KillrVideo.Search.InternalMessages;
using Nimbus;

namespace KillrVideo.Search.SearchImpl
{
    /// <summary>
    /// A component selector for Castle Windsor that selects which search service implementation to use.
    /// </summary>
    public class SearchComponentSelector : DefaultTypedFactoryComponentSelector
    {
        private static readonly Type EnterpriseSearchType = typeof (DataStaxEnterpriseSearch);
        private static readonly Type TagSearchType = typeof (SearchVideosByTag);

        private readonly IBus _bus;
        private readonly Lazy<Task<bool>> _useEnterpriseSearch;

        public SearchComponentSelector(IBus bus)
        {
            if (bus == null) throw new ArgumentNullException("bus");
            _bus = bus;
            _useEnterpriseSearch = new Lazy<Task<bool>>(CanUseEnterpriseSearch);
        }

        protected override Type GetComponentType(MethodInfo method, object[] arguments)
        {
            // If the request to check is still in flight, or if it finished and said enterprise search was not available
            if (_useEnterpriseSearch.Value.IsCompleted == false || _useEnterpriseSearch.Value.Result == false)
                return TagSearchType;

            return EnterpriseSearchType;
        }

        private async Task<bool> CanUseEnterpriseSearch()
        {
            try
            {
                // Ask the search worker if DSE search is available
                EnterpriseSearchAvailability response = await _bus.Request(new CheckForEnterpriseSearch(), TimeSpan.FromSeconds(60)).ConfigureAwait(false);
                return response.IsAvailable;
            }
            catch (Exception)
            {
                // If something goes wrong, just assume it's not available
                return false;
            }
        }
    }
}
