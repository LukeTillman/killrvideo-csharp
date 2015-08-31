using System;
using KillrVideo.Models.Shared;

namespace KillrVideo.Models.Account
{
    /// <summary>
    /// Represents the account information for the currently logged in user.
    /// </summary>
    [Serializable]
    public class CurrentAccountViewModel
    {
        /// <summary>
        /// Whether or not someone is currently logged in.
        /// </summary>
        public bool IsLoggedIn { get; set; }

        /// <summary>
        /// The profile for the current user, or null if not logged in.
        /// </summary>
        public UserProfileViewModel Profile { get; set; }
    }
}