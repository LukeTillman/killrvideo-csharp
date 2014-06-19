using System;
using KillrVideo.Models.Shared;

namespace KillrVideo.Models.Account
{
    /// <summary>
    /// View model for the Account Info view.
    /// </summary>
    [Serializable]
    public class AccountInfoViewModel
    {
        public UserProfileViewModel UserProfile { get; set; }
        public bool IsCurrentlyLoggedInUser { get; set; }
    }
}