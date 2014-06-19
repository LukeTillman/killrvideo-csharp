using System;

namespace KillrVideo.Models.Account
{
    /// <summary>
    /// View model for requesting account info.
    /// </summary>
    [Serializable]
    public class GetAccountInfoViewModel
    {
        public Guid? UserId { get; set; }
    }
}