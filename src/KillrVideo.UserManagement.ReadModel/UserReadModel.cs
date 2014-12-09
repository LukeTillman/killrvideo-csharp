using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.UserManagement.ReadModel.Dtos;
using KillrVideo.Utils;

namespace KillrVideo.UserManagement.ReadModel
{
    /// <summary>
    /// Handles queries related to users using the core Cassandra driver (Statements, Session, etc.)
    /// </summary>
    public class UserReadModel : IUserReadModel
    {
        private readonly ISession _session;
        private readonly TaskCache<string, PreparedStatement> _statementCache;

        public UserReadModel(ISession session, TaskCache<string, PreparedStatement> statementCache)
        {
            if (session == null) throw new ArgumentNullException("session");
            if (statementCache == null) throw new ArgumentNullException("statementCache");
            _session = session;
            _statementCache = statementCache;
        }

        /// <summary>
        /// Gets user credentials by email address.
        /// </summary>
        public async Task<UserCredentials> GetCredentials(string emailAddress)
        {
            PreparedStatement preparedStatement =
                await _statementCache.NoContext.GetOrAddAsync("SELECT email, password, userid FROM user_credentials WHERE email = ?");

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
            PreparedStatement preparedStatement =
                await _statementCache.NoContext.GetOrAddAsync("SELECT userid, firstname, lastname, email FROM users WHERE userid = ?");

            // We should get a single row back for a user id, or null
            RowSet resultRows = await _session.ExecuteAsync(preparedStatement.Bind(userId)).ConfigureAwait(false);
            return MapRowToUserProfile(resultRows.SingleOrDefault());
        }

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