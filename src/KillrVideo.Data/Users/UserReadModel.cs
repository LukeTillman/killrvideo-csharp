using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.Data.Users.Dtos;

namespace KillrVideo.Data.Users
{
    /// <summary>
    /// Handles queries related to users using the core Cassandra driver (Statements, Session, etc.)
    /// </summary>
    public class UserReadModel : IUserReadModel
    {
        private readonly ISession _session;

        // Reusable, lazy-evaluated prepared statements
        private readonly AsyncLazy<PreparedStatement> _getCredentials;
        private readonly AsyncLazy<PreparedStatement> _getUserProfileById; 

        public UserReadModel(ISession session)
        {
            if (session == null) throw new ArgumentNullException("session");
            _session = session;

            _getCredentials =
                new AsyncLazy<PreparedStatement>(() => _session.PrepareAsync("SELECT email, password, userid FROM user_credentials WHERE email = ?"));
            _getUserProfileById =
                new AsyncLazy<PreparedStatement>(() => _session.PrepareAsync("SELECT userid, firstname, lastname, email FROM users WHERE userid = ?"));
        }

        /// <summary>
        /// Gets user credentials by email address.
        /// </summary>
        public async Task<UserCredentials> GetCredentials(string emailAddress)
        {
            PreparedStatement preparedStatement = await _getCredentials;

            // Use the get credentials prepared statement to find credentials for the user
            RowSet result = await _session.ExecuteAsync(preparedStatement.Bind(emailAddress)).ConfigureAwait(false);

            // We should get a single credentials result or no results
            Row row = result.SingleOrDefault();
            if (row == null)
                return null;

            // Map row to UserCredentials object
            return new UserCredentials
            {
                EmailAddress = row.GetValue<string>("email"),
                Password = row.GetValue<string>("password"),
                UserId = row.GetValue<Guid>("userid")
            };
        }

        public async Task<UserProfile> GetUserProfile(Guid userId)
        {
            PreparedStatement preparedStatement = await _getUserProfileById;

            // We should get a single row back for a user id, or null
            RowSet resultRows = await _session.ExecuteAsync(preparedStatement.Bind(userId)).ConfigureAwait(false);
            return MapRowToUserProfile(resultRows.SingleOrDefault());
        }

        public async Task<IEnumerable<UserProfile>> GetUserProfiles(ISet<Guid> userIds)
        {
            if (userIds == null) return Enumerable.Empty<UserProfile>();

            // Since we're essentially doing a multi-get here, limit the number userIds (i.e. partition keys) to 20 in an attempt
            // to enforce some performance sanity.  Anything larger and we might want to consider a different data model that doesn't 
            // involve doing a multi-get
            if (userIds.Count > 20) throw new ArgumentOutOfRangeException("userIds", "Cannot do multi-get on more than 20 user id keys.");

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