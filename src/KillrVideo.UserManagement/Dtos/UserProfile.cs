using System;
using Cassandra.Data.Linq;

namespace KillrVideo.UserManagement.Dtos
{
    /// <summary>
    /// Represents a user's profile information.  The Table/Column/PartitionKey attributes are here only for LINQ support (see LinqUserReadModel).
    /// </summary>
    [Serializable]
    [Table("users")]
    public class UserProfile
    {
        [PartitionKey]
        [Column("userid")]
        public Guid UserId { get; set; }

        [Column("firstname")]
        public string FirstName { get; set; }

        [Column("lastname")]
        public string LastName { get; set; }

        [Column("email")]
        public string EmailAddress { get; set; }
    }
}
