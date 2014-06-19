using System;
using System.Security.Principal;

namespace KillrVideo.Authentication
{
    /// <summary>
    /// Extension methods for IPrincipal.
    /// </summary>
    public static class PrincipalExtensions
    {
        /// <summary>
        /// Gets the currently logged in user id from the principal, or null if no user is logged in.
        /// </summary>
        public static Guid? GetCurrentUserId(this IPrincipal user)
        {
            if (user == null) return null;
            if (user.Identity == null) return null;
            if (user.Identity.IsAuthenticated == false) return null;

            return Guid.Parse(user.Identity.Name);
        }
    }
}