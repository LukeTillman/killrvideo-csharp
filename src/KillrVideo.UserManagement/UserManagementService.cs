using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.UserManagement.Dtos;
using KillrVideo.UserManagement.Messages.Events;
using KillrVideo.Utils;
using Nimbus;

namespace KillrVideo.UserManagement
{
    /// <summary>
    /// An implementation of the user management service that stores accounts in Cassandra and publishes events on a message bus.
    /// </summary>
    public class UserManagementService : IUserManagementService
    {
        private readonly ISession _session;
        private readonly TaskCache<string, PreparedStatement> _statementCache;
        private readonly IBus _bus;

        public UserManagementService(ISession session, TaskCache<string, PreparedStatement> statementCache, IBus bus)
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

            DateTimeOffset timestamp = DateTimeOffset.UtcNow;

            PreparedStatement preparedCredentials = await _statementCache.NoContext.GetOrAddAsync(
                "INSERT INTO user_credentials (email, password, userid) VALUES (?, ?, ?) IF NOT EXISTS USING TIMESTAMP ?");

            // Insert the credentials info (this will return false if a user with that email address already exists)
            IStatement insertCredentialsStatement = preparedCredentials.Bind(user.EmailAddress, hashedPassword, user.UserId,
                                                                             timestamp.ToMicrosecondsSinceEpoch());
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
            });
        }

        /// <summary>
        /// Verifies a user's credentials and returns the user's Id if successful, otherwise null.
        /// </summary>
        public async Task<Guid?> VerifyCredentials(string emailAddress, string password)
        {
            PreparedStatement preparedStatement =
                await _statementCache.NoContext.GetOrAddAsync("SELECT email, password, userid FROM user_credentials WHERE email = ?");

            // Use the get credentials prepared statement to find credentials for the user
            RowSet result = await _session.ExecuteAsync(preparedStatement.Bind(emailAddress)).ConfigureAwait(false);

            // We should get a single credentials result or no results
            Row row = result.SingleOrDefault();
            if (row == null)
                return null;

            // Verify the password hash
            if (PasswordHash.ValidatePassword(password, row.GetValue<string>("password")) == false)
                return null;

            return row.GetValue<Guid>("userid");
        }
        
        /// <summary>
        /// Gets a user's profile information by their user Id.  Returns null if they cannot be found.
        /// </summary>
        public async Task<UserProfile> GetUserProfile(Guid userId)
        {
            PreparedStatement preparedStatement =
                await _statementCache.NoContext.GetOrAddAsync("SELECT userid, firstname, lastname, email FROM users WHERE userid = ?");

            // We should get a single row back for a user id, or null
            RowSet resultRows = await _session.ExecuteAsync(preparedStatement.Bind(userId)).ConfigureAwait(false);
            return MapRowToUserProfile(resultRows.SingleOrDefault());
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

            // As an example, we'll do the multi-get at the CQL level using an IN() clause (i.e. let Cassandra handle it).  For an example of
            // doing it at the driver level, see the VideoReadModel.GetVideoPreviews method

            // Build a parameterized CQL statement with an IN clause
            var parameterList = string.Join(", ", Enumerable.Repeat("?", userIds.Count));
            var statement =
                new SimpleStatement(string.Format("SELECT userid, firstname, lastname, email FROM users WHERE userid IN ({0})", parameterList));
            statement.Bind(userIds.Cast<object>().ToArray());

            // Execute and map to UserProfile object
            RowSet resultRows = await _session.ExecuteAsync(statement).ConfigureAwait(false);
            return resultRows.Select(MapRowToUserProfile).ToList();
        }

        private static UserProfile MapRowToUserProfile(Row row)
        {
            if (row == null) return null;

            return new UserProfile
            {
                UserId = row.GetValue<Guid>("userid"),
                FirstName = row.GetValue<string>("firstname"),
                LastName = row.GetValue<string>("lastname"),
                EmailAddress = row.GetValue<string>("email")
            };
        }
    }
}