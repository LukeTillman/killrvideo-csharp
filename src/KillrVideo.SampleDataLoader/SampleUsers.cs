using System;
using KillrVideo.Data.Users.Dtos;

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
                UserId = Guid.NewGuid(),
                EmailAddress = "user1@killrvideo.com",
                Password = PasswordHash.CreateHash("Password1!"),
                FirstName = "User",
                LastName = "One"
            },
            new CreateUser
            {
                UserId = Guid.NewGuid(),
                EmailAddress = "user2@killrvideo.com",
                Password = PasswordHash.CreateHash("Password2!"),
                FirstName = "User",
                LastName = "Two"
            },
            new CreateUser
            {
                UserId = Guid.NewGuid(),
                EmailAddress = "user3@killrvideo.com",
                Password = PasswordHash.CreateHash("Password3!"),
                FirstName = "User",
                LastName = "Three"
            },
            new CreateUser
            {
                UserId = Guid.NewGuid(),
                EmailAddress = "user4@killrvideo.com",
                Password = PasswordHash.CreateHash("Password4!"),
                FirstName = "User",
                LastName = "Four"
            }
        };
    }
}
