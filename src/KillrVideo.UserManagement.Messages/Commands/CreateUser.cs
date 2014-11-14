using System;
using Nimbus.MessageContracts;

namespace KillrVideo.UserManagement.Messages.Commands
{
    /// <summary>
    /// DTO for creating a new user account.
    /// </summary>
    public class CreateUser : IBusCommand
    {
        public Guid UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmailAddress { get; set; }
        public string Password { get; set; }
    }
}
