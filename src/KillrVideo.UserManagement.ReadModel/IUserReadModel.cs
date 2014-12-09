using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KillrVideo.UserManagement.ReadModel.Dtos;

namespace KillrVideo.UserManagement.ReadModel
{
    public interface IUserReadModel
    {
        /// <summary>
        /// Gets user credentials by email address.  Returns null if they cannot be found.
        /// </summary>
        Task<UserCredentials> GetCredentials(string emailAddress);

        /// <summary>
        /// Gets a user's profile information by their user Id.  Returns null if they cannot be found.
        /// </summary>
        Task<UserProfile> GetUserProfile(Guid userId);

        /// <summary>
        /// Gets multiple user profiles by their Ids.  
        /// </summary>
        Task<IEnumerable<UserProfile>> GetUserProfiles(ISet<Guid> userIds);
    }
}