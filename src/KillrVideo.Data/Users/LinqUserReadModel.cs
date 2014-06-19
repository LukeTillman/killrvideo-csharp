using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cassandra;
using Cassandra.Data.Linq;
using KillrVideo.Data.Users.Dtos;

namespace KillrVideo.Data.Users
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
        public Task<IEnumerable<UserProfile>> GetUserProfiles(ISet<Guid> userIds)
        {
            // TODO:  Does LINQ driver support multi-get CONTAINS type query or do we have to do it client side?
            throw new NotImplementedException();
        }
    }
}
