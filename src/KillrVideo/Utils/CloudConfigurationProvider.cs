using System.Configuration;
using KillrVideo.Utils.Configuration;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace KillrVideo.Utils
{
    /// <summary>
    /// Provides configuration values from Azure.
    /// </summary>
    public class CloudConfigurationProvider : IGetEnvironmentConfiguration
    {
        private readonly string _uniqueInstanceId;
        private readonly string _appName;

        /// <summary>
        /// The name of the currently running application.
        /// </summary>
        public string AppName
        {
            get { return _appName; }
        }

        /// <summary>
        /// Gets a unique Id for the running app.
        /// </summary>
        public string UniqueInstanceId
        {
            get { return _uniqueInstanceId; }
        }

        public CloudConfigurationProvider()
        {
            _uniqueInstanceId = RoleEnvironment.CurrentRoleInstance.Id;
            _appName = typeof (CloudConfigurationProvider).Assembly.GetName().Name;
        }

        /// <summary>
        /// Gets a configuration setting's value for the given key.
        /// </summary>
        public string GetSetting(string key)
        {
            var value = CloudConfigurationManager.GetSetting(key);
            if (string.IsNullOrEmpty(value))
                throw new ConfigurationErrorsException(string.Format("No value for required setting {0} in cloud configuration", key));

            return value;
        }
    }
}
