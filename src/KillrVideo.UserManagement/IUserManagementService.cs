using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KillrVideo.UserManagement.Dtos;

namespace KillrVideo.UserManagement
{
    /// <summary>
    /// The public API for the user management service.
    /// </summary>
    public interface IUserManagementService
    {
        /// <summary>
        /// Creates a new user account.
        /// </summary>
        Task CreateUser(CreateUser user);

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
