using System;

namespace KillrVideo.UserManagement.Dtos
{
    /// <summary>
    /// DTO for creating a new user account.
    /// </summary>
    [Serializable]
    public class CreateUser
    {
        public Guid UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmailAddress { get; set; }

        /// <summary>
        /// The plain text password for the new user.
        /// </summary>
        public string Password { get; set; }
    }
}
