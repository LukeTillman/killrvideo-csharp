using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cassandra;
using Cassandra.Data.Linq;
using KillrVideo.UserManagement.Dtos;

namespace KillrVideo.UserManagement
{
    /// <summary>
    /// Handles queries related to users using the LINQ to CQL driver.
    /// </summary>
    public class LinqUserReadModel : IUserReadModel
    {
        private readonly ISession _session;

        public LinqUserReadModel(ISession session)
        {
            if (session == null) throw new ArgumentNullException("session");
            _session = session;
        }

        /// <summary>
        /// Gets user credentials by email address.
        /// </summary>
        public async Task<UserCredentials> GetCredentials(string emailAddress)
        {
            IEnumerable<UserCredentials> results = await _session.GetTable<UserCredentials>()
                                                                 .Where(uc => uc.EmailAddress == emailAddress)
                                                                 .ExecuteAsync().ConfigureAwait(false);
            return results.SingleOrDefault();
        }

        /// <summary>
        /// Gets a user's profile information by their user Id.  Returns null if they cannot be found.
        /// </summary>
        public async Task<UserProfile> GetUserProfile(Guid userId)
        {
            IEnumerable<UserProfile> results = await _session.GetTable<UserProfile>()
                                                             .Where(up => up.UserId == userId)
                                                             .ExecuteAsync().ConfigureAwait(false);
            return results.SingleOrDefault();
        }

        /// <summary>
        /// Gets multiple user profiles by their Ids.  
        /// </summary>
        public async Task<IEnumerable<UserProfile>> GetUserProfiles(ISet<Guid> userIds)
        {
            if (userIds == null || userIds.Count == 0) return Enumerable.Empty<UserProfile>();

            // Since we're essentially doing a multi-get here, limit the number userIds (i.e. partition keys) to 20 in an attempt
            // to enforce some performance sanity.  Anything larger and we might want to consider a different data model that doesn't 
            // involve doing a multi-get
            if (userIds.Count > 20) throw new ArgumentOutOfRangeException("userIds", "Cannot do multi-get on more than 20 user id keys.");

            IEnumerable<UserProfile> results = await _session.GetTable<UserProfile>()
                                                             .Where(up => userIds.Contains(up.UserId))
                                                             .ExecuteAsync().ConfigureAwait(false);
            return results;
        }
    }
}
