using System;

namespace KillrVideo.Models.Account
{
    /// <summary>
    /// Model for registering a new user with the site.
    /// </summary>
    [Serializable]
    public class RegisterUserViewModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmailAddress { get; set; }
        public string Password { get; set; }
    }
}