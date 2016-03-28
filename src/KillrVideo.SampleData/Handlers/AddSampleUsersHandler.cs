using System;
using System.Linq;
using System.Threading.Tasks;
using Cassandra;
using Faker;
using KillrVideo.MessageBus;
using KillrVideo.Utils;

namespace KillrVideo.SampleData.Handlers
{
    /// <summary>
    /// Adds sample users to the site.
    /// </summary>
    public class AddSampleUsersHandler : IHandleMessage<AddSampleUsersRequest>
    {
        private readonly ISession _session;
        private readonly TaskCache<string, PreparedStatement> _statementCache;
        private readonly IUserManagementService _userManagement;

        public AddSampleUsersHandler(ISession session, TaskCache<string, PreparedStatement> statementCache, IUserManagementService userManagement)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (statementCache == null) throw new ArgumentNullException(nameof(statementCache));
            if (userManagement == null) throw new ArgumentNullException(nameof(userManagement));
            _session = session;
            _statementCache = statementCache;
            _userManagement = userManagement;
        }

        public async Task Handle(AddSampleUsersRequest busCommand)
        {
            // Create some fake user data
            var users = Enumerable.Range(0, busCommand.NumberOfUsers).Select(idx =>
            {
                var user = new CreateUser
                {
                    UserId = Guid.NewGuid(),
                    FirstName = Name.First(),
                    LastName = Name.Last(),
                    Password = Internet.Password(7, 20)
                };
                user.EmailAddress = $"{Internet.UserName($"{user.FirstName} {user.LastName}")}+sampleuser@killrvideo.com";
                return user;
            }).ToArray();

            // Get statement for recording sample users
            PreparedStatement prepared = await _statementCache.NoContext.GetOrAddAsync("INSERT INTO sample_data_users (userid) VALUES (?)");

            // Add the user and record their Id in C* (not a big deal if we fail halfway through since this is sample data)
            foreach (CreateUser user in users)
            {
                await _userManagement.CreateUser(user).ConfigureAwait(false);
                BoundStatement bound = prepared.Bind(user.UserId);
                await _session.ExecuteAsync(bound).ConfigureAwait(false);
            }
        }
    }
}