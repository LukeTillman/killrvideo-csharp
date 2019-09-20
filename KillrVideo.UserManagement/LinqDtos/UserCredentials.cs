using System;
using Cassandra.Mapping.Attributes;

namespace KillrVideo.UserManagement.LinqDtos
{
    /// <summary>
    /// Represents user credentials.  The Table/Column/PartitionKey attributes are here only for LINQ support (see LinqUserReadModel).
    /// </summary>
    [Serializable]
    [Table("user_credentials")]
    public class UserCredentials
    {
        [Column("email")]
        [PartitionKey]
        public string EmailAddress { get; set; }

        [Column("password")]
        public string Password { get; set; }

        [Column("userid")]
        public Guid UserId { get; set; }
    }
}
