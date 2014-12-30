using System;
using System.Linq;
using System.Threading.Tasks;
using Cassandra;
using Faker;
using Faker.Extensions;
using KillrVideo.SampleData.Dtos;
using KillrVideo.UserManagement;
using KillrVideo.UserManagement.Dtos;
using KillrVideo.Utils;
using Nimbus.Handlers;

namespace KillrVideo.SampleData.Worker.Handlers
{
    /// <summary>
    /// Adds sample users to the site.
    /// </summary>
    public class AddSampleUsersHandler : IHandleCommand<AddSampleUsers>
    {
        private const string PasswordPattern = "?#??#???#??#?";

        private readonly ISession _session;
        private readonly TaskCache<string, PreparedStatement> _statementCache;
        private readonly IUserManagementService _userManagement;

        public AddSampleUsersHandler(ISession session, TaskCache<string, PreparedStatement> statementCache, IUserManagementService userManagement)
        {
            if (session == null) throw new ArgumentNullException("session");
            if (statementCache == null) throw new ArgumentNullException("statementCache");
            if (userManagement == null) throw new ArgumentNullException("userManagement");
            _session = session;
            _statementCache = statementCache;
            _userManagement = userManagement;
        }

        public async Task Handle(AddSampleUsers busCommand)
        {
            // Create some fake user data
            var users = Enumerable.Range(0, busCommand.NumberOfUsers).Select(idx =>
            {
                var user = new CreateUser
                {
                    UserId = Guid.NewGuid(),
                    FirstName = Name.GetFirstName(),
                    LastName = Name.GetLastName(),
                    Password = PasswordPattern.Bothify()
                };
                user.EmailAddress = string.Format("{0}+sampleuser@killrvideo.com", Internet.GetUserName(user.FirstName, user.LastName));
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