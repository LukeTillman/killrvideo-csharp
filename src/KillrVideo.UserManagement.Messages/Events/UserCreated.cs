using System;
using Nimbus.MessageContracts;

namespace KillrVideo.UserManagement.Messages.Events
{
    /// <summary>
    /// Indicates a new user registered with the site.
    /// </summary>
    [Serializable]
    public class UserCreated : IBusEvent
    {
        public Guid UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmailAddress { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }
}
