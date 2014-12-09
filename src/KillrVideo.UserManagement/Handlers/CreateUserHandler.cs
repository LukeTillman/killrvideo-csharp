using System;
using System.Linq;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.UserManagement.Messages.Commands;
using KillrVideo.UserManagement.Messages.Events;
using KillrVideo.Utils;
using Nimbus;
using Nimbus.Handlers;

namespace KillrVideo.UserManagement.Handlers
{
    /// <summary>
    /// Creates new users.
    /// </summary>
    public class CreateUserHandler : IHandleCommand<CreateUser>
    {
        private readonly ISession _session;
        private readonly TaskCache<string, PreparedStatement> _statementCache;
        private readonly IBus _bus;

        public CreateUserHandler(ISession session, TaskCache<string, PreparedStatement> statementCache, IBus bus)
        {
            if (session == null) throw new ArgumentNullException("session");
            if (statementCache == null) throw new ArgumentNullException("statementCache");
            _session = session;
            _statementCache = statementCache;
            _bus = bus;
        }

        public async Task Handle(CreateUser user)
        {
            DateTimeOffset timestamp = DateTimeOffset.UtcNow;

            PreparedStatement preparedCredentials = await _statementCache.NoContext.GetOrAddAsync(
                "INSERT INTO user_credentials (email, password, userid) VALUES (?, ?, ?) IF NOT EXISTS");

            // Insert the credentials info (this will return false if a user with that email address already exists)
            IStatement insertCredentialsStatement = preparedCredentials.Bind(user.EmailAddress, user.Password, user.UserId).SetTimestamp(timestamp);
            RowSet credentialsResult = await _session.ExecuteAsync(insertCredentialsStatement).ConfigureAwait(false);

            // The first column in the row returned will be a boolean indicating whether the change was applied (TODO: Compensating action for user creation failure?)
            var applied = credentialsResult.Single().GetValue<bool>("[applied]");
            if (applied == false)
                return;

            PreparedStatement preparedUser = await _statementCache.NoContext.GetOrAddAsync(
                "INSERT INTO users (userid, firstname, lastname, email, created_date) VALUES (?, ?, ?, ?, ?)");

            // Insert the "profile" information using a parameterized CQL statement
            IStatement insertUserStatement =
                preparedUser.Bind(user.UserId, user.FirstName, user.LastName, user.EmailAddress, timestamp).SetTimestamp(timestamp);

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
    }
}
