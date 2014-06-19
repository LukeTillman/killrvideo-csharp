using System;
using KillrVideo.Models.Shared;

namespace KillrVideo.Models.Home
{
    /// <summary>
    /// View model for the navbar.
    /// </summary>
    [Serializable]
    public class ViewNavbarViewModel
    {
        /// <summary>
        /// The currently logged in user, or null if no one is logged in.
        /// </summary>
        public UserProfileViewModel LoggedInUser { get; set; }
    }
}