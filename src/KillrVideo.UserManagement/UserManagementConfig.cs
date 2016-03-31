using System.Collections.Generic;

namespace KillrVideo.UserManagement
{
    /// <summary>
    /// Configuration helper for the User Management service.
    /// </summary>
    public static class UserManagementConfig
    {
        /// <summary>
        /// Configuration key that determines whether or not to use the LINQ User Management service implementation.
        /// </summary>
        public const string UseLinqKey = "UserManagement.UseLinq";

        /// <summary>
        /// Returns true if the LINQ implementation should be used.
        /// </summary>
        internal static bool UseLinq(IDictionary<string, string> config)
        {
            string useLinq;
            if (config.TryGetValue(UseLinqKey, out useLinq) == false)
                return false;

            return bool.Parse(useLinq);
        }
    }
}
