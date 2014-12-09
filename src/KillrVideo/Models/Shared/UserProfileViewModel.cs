using System;
using KillrVideo.UserManagement.ReadModel.Dtos;
using KillrVideo.Utils;

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

        /// <summary>
        /// Creates a ViewModel instance from the common user profile model returned from the data layer.
        /// </summary>
        public static UserProfileViewModel FromDataModel(UserProfile data)
        {
            if (data == null) return null;

            return new UserProfileViewModel
            {
                UserId = data.UserId,
                EmailAddress = data.EmailAddress,
                FirstName = data.FirstName,
                LastName = data.LastName,
                GravatarHash = GravatarHasher.GetHashForEmailAddress(data.EmailAddress)
            };
        }
    }
}