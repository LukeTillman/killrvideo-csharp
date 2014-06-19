using System;

namespace KillrVideo.Models.Account
{
    /// <summary>
    /// Model for signing in a user.
    /// </summary>
    [Serializable]
    public class SignInUserViewModel
    {
        public string EmailAddress { get; set; }
        public string Password { get; set; }
    }
}