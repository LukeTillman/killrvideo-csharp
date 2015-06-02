using System;
using System.Reflection;
using Castle.Facilities.TypedFactory;
using KillrVideo.Utils.Configuration;

namespace KillrVideo.Search.SearchImpl
{
    /// <summary>
    /// A component selector for Castle Windsor that selects which search service implementation to use.
    /// </summary>
    public class SearchComponentSelector : DefaultTypedFactoryComponentSelector
    {
        private const string EnterpriseSearchConfigKey = "EnterpriseSearchEnabled";

        private static readonly Type EnterpriseSearchType = typeof (DataStaxEnterpriseSearch);
        private static readonly Type TagSearchType = typeof (SearchVideosByTag);

        private readonly IGetEnvironmentConfiguration _configRetriever;
        private readonly Lazy<bool> _useEnterpriseSearch;

        public SearchComponentSelector(IGetEnvironmentConfiguration configRetriever)
        {
            if (configRetriever == null) throw new ArgumentNullException("configRetriever");
            _configRetriever = configRetriever;
            _useEnterpriseSearch = new Lazy<bool>(CanUseEnterpriseSearch);
        }

        protected override Type GetComponentType(MethodInfo method, object[] arguments)
        {
            return _useEnterpriseSearch.Value ? EnterpriseSearchType : TagSearchType;
        }

        private bool CanUseEnterpriseSearch()
        {
            try
            {
                // Use a flag in the configuration settings to determine whether to use DSE search
                string enabled = _configRetriever.GetSetting(EnterpriseSearchConfigKey);
                return bool.Parse(enabled);
            }
            catch (Exception)
            {
                // If something goes wrong, just assume it's not available
                return false;
            }
        }
    }
}
