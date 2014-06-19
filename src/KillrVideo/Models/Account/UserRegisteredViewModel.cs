using System;

namespace KillrVideo.Models.Account
{
    /// <summary>
    /// Response model for a successful user registration.
    /// </summary>
    [Serializable]
    public class UserRegisteredViewModel
    {
        /// <summary>
        /// The user Id of the new user that was registered.
        /// </summary>
        public Guid UserId { get; set; }
    }
}