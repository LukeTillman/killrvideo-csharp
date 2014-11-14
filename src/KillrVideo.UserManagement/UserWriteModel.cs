using System;
using System.Linq;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.UserManagement.Messages.Commands;
using KillrVideo.Utils;

namespace KillrVideo.UserManagement
{
    /// <summary>
    /// Handles writes/updates related to users.
    /// </summary>
    public class UserWriteModel : IUserWriteModel
    {
        private readonly ISession _session;
        
        // Some reusable prepared statements, lazy initialized as needed
        private readonly AsyncLazy<PreparedStatement> _insertUserStatement;
        private readonly AsyncLazy<PreparedStatement> _insertCredentialsStatement;

        public UserWriteModel(ISession session)
        {
            if (session == null) throw new ArgumentNullException("session");
            _session = session;
            
            // Initialize some lazy-prepared statements for reuse
            _insertUserStatement = new AsyncLazy<PreparedStatement>(
                () => _session.PrepareAsync("INSERT INTO users (userid, firstname, lastname, email, created_date) VALUES (?, ?, ?, ?, ?)"));
            _insertCredentialsStatement = new AsyncLazy<PreparedStatement>(
                () => _session.PrepareAsync("INSERT INTO user_credentials (email, password, userid) VALUES (?, ?, ?) IF NOT EXISTS"));
        }

        /// <summary>
        /// Creates a new user.  Returns true if successful or false if a user with the email address specified already exists.
        /// </summary>
        public async Task<bool> CreateUser(CreateUser user)
        {
            PreparedStatement preparedCredentials = await _insertCredentialsStatement;

            // Insert the credentials info (this will return false if a user with that email address already exists)
            BoundStatement insertCredentialsStatement = preparedCredentials.Bind(user.EmailAddress, user.Password, user.UserId);
            RowSet credentialsResult = await _session.ExecuteAsync(insertCredentialsStatement).ConfigureAwait(false);

            // The first column in the row returned will be a boolean indicating whether the change was applied
            var applied = credentialsResult.Single().GetValue<bool>("[applied]");
            if (applied == false)
                return false;

            PreparedStatement preparedUser = await _insertUserStatement;
            
            // Insert the "profile" information using a parameterized CQL statement
            BoundStatement insertUserStatement = preparedUser.Bind(user.UserId, user.FirstName, user.LastName, user.EmailAddress,
                                                                   DateTimeOffset.UtcNow);

            await _session.ExecuteAsync(insertUserStatement).ConfigureAwait(false);

            return true;
        }
    }
}
