using System;

namespace KillrVideo.Models.Shared
{
    [Serializable]
    public class UserProfileViewModel
    {
        public Guid UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmailAddress { get; set; }
        public string GravatarHash { get; set; }
    }
}