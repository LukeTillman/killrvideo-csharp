using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cassandra;
using Cassandra.Data.Linq;
using KillrVideo.UserManagement.Dtos;
using KillrVideo.UserManagement.Messages.Events;
using KillrVideo.Utils;
using Nimbus;

namespace KillrVideo.UserManagement
{
    /// <summary>
    /// An implementation of the user management service that uses the LINQ portion of the Cassandra driver to store user accounts in Cassandra
    /// and publishes events to a message bus.
    /// </summary>
    public class LinqUserManagementService : IUserManagementService
    {
        private readonly ISession _session;
        private readonly TaskCache<string, PreparedStatement> _statementCache;
        private readonly IBus _bus;

        public LinqUserManagementService(ISession session, TaskCache<string, PreparedStatement> statementCache, IBus bus)
        {
            if (session == null) throw new ArgumentNullException("session");
            if (statementCache == null) throw new ArgumentNullException("statementCache");
            _session = session;
            _statementCache = statementCache;
            _bus = bus;
        }

        /// <summary>
        /// Creates a new user account.
        /// </summary>
        public async Task CreateUser(CreateUser user)
        {
            // Hash the user's password
            string hashedPassword = PasswordHash.CreateHash(user.Password);

            // TODO:  Use LINQ to create users
            DateTimeOffset timestamp = DateTimeOffset.UtcNow;

            PreparedStatement preparedCredentials = await _statementCache.NoContext.GetOrAddAsync(
                "INSERT INTO user_credentials (email, password, userid) VALUES (?, ?, ?) IF NOT EXISTS");

            // Insert the credentials info (this will return false if a user with that email address already exists)
            IStatement insertCredentialsStatement = preparedCredentials.Bind(user.EmailAddress, hashedPassword, user.UserId);
            RowSet credentialsResult = await _session.ExecuteAsync(insertCredentialsStatement).ConfigureAwait(false);

            // The first column in the row returned will be a boolean indicating whether the change was applied (TODO: Compensating action for user creation failure?)
            var applied = credentialsResult.Single().GetValue<bool>("[applied]");
            if (applied == false)
                return;

            PreparedStatement preparedUser = await _statementCache.NoContext.GetOrAddAsync(
                "INSERT INTO users (userid, firstname, lastname, email, created_date) VALUES (?, ?, ?, ?, ?) USING TIMESTAMP ?");

            // Insert the "profile" information using a parameterized CQL statement
            IStatement insertUserStatement =
                preparedUser.Bind(user.UserId, user.FirstName, user.LastName, user.EmailAddress, timestamp, timestamp.ToMicrosecondsSinceEpoch());

            await _session.ExecuteAsync(insertUserStatement).ConfigureAwait(false);

            // Tell the world about the new user
            await _bus.Publish(new UserCreated
            {
                UserId = user.UserId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                EmailAddress = user.EmailAddress,
                Timestamp = timestamp
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Verifies a user's credentials and returns the user's Id if successful, otherwise null.
        /// </summary>
        public async Task<Guid?> VerifyCredentials(string emailAddress, string password)
        {
            // Lookup the user by email address
            IEnumerable<UserCredentials> results = await _session.GetTable<UserCredentials>()
                                                                 .Where(uc => uc.EmailAddress == emailAddress)
                                                                 .ExecuteAsync().ConfigureAwait(false);
            
            // Make sure we found a user
            UserCredentials credentials = results.SingleOrDefault();
            if (credentials == null)
                return null;

            // Verify the password hash
            if (PasswordHash.ValidatePassword(password, credentials.Password) == false)
                return null;

            return credentials.UserId;
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