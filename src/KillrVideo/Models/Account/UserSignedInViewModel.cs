using System;

namespace KillrVideo.Models.Account
{
    /// <summary>
    /// Response model for when a user is successfully signed in.
    /// </summary>
    [Serializable]
    public class UserSignedInViewModel
    {
        public string AfterLoginUrl { get; set; }
    }
}