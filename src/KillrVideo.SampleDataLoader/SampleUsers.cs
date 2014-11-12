using System;
using KillrVideo.UserManagement.Api.Commands;

namespace KillrVideo.SampleDataLoader
{
    /// <summary>
    /// Contains sample user data for populating the KillrVideo schema.
    /// </summary>
    public static class SampleUsers
    {
        public static readonly CreateUser[] Data =
        {
            new CreateUser
            {
                UserId = Guid.Parse("359c7d9d-b921-4c91-bf8b-effc8173b144"),
                EmailAddress = "user1@killrvideo.com",
                Password = PasswordHash.CreateHash("Password1!"),
                FirstName = "User",
                LastName = "One"
            },
            new CreateUser
            {
                UserId = Guid.Parse("319c4e89-6b01-426c-93dc-292f080332af"),
                EmailAddress = "user2@killrvideo.com",
                Password = PasswordHash.CreateHash("Password2!"),
                FirstName = "User",
                LastName = "Two"
            },
            new CreateUser
            {
                UserId = Guid.Parse("442a8bc0-2a2c-44f8-8b41-d9d2a4dc7449"),
                EmailAddress = "user3@killrvideo.com",
                Password = PasswordHash.CreateHash("Password3!"),
                FirstName = "User",
                LastName = "Three"
            },
            new CreateUser
            {
                UserId = Guid.Parse("7289fabe-3634-456e-951c-031d2059c910"),
                EmailAddress = "user4@killrvideo.com",
                Password = PasswordHash.CreateHash("Password4!"),
                FirstName = "User",
                LastName = "Four"
            }
        };
    }
}
