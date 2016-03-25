using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cassandra;
using Cassandra.Data.Linq;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using KillrVideo.MessageBus;
using KillrVideo.Protobuf;
using KillrVideo.UserManagement.Events;
using KillrVideo.UserManagement.LinqDtos;
using KillrVideo.Utils;

namespace KillrVideo.UserManagement
{
    /// <summary>
    /// An implementation of the user management service that uses the LINQ portion of the Cassandra driver to store user accounts in Cassandra
    /// and publishes events to a message bus.
    /// </summary>
    public class LinqUserManagementService : UserManagementService.IUserManagementService
    {
        private readonly ISession _session;
        private readonly TaskCache<string, PreparedStatement> _statementCache;
        private readonly IBus _bus;

        private readonly Table<LinqDtos.UserProfile> _userProfileTable;
        private readonly Table<UserCredentials> _userCredentialsTable; 

        public LinqUserManagementService(ISession session, TaskCache<string, PreparedStatement> statementCache, IBus bus)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (statementCache == null) throw new ArgumentNullException(nameof(statementCache));
            if (bus == null) throw new ArgumentNullException(nameof(bus));

            _session = session;
            _statementCache = statementCache;
            _bus = bus;

            _userProfileTable = new Table<LinqDtos.UserProfile>(session);
            _userCredentialsTable = new Table<UserCredentials>(session);
        }

        /// <summary>
        /// Creates a new user account.
        /// </summary>
        public async Task<CreateUserResponse> CreateUser(CreateUserRequest request, ServerCallContext context)
        {
            // Hash the user's password
            string hashedPassword = PasswordHash.CreateHash(request.Password);

            // TODO:  Use LINQ to create users
            DateTimeOffset timestamp = DateTimeOffset.UtcNow;

            PreparedStatement preparedCredentials = await _statementCache.NoContext.GetOrAddAsync(
                "INSERT INTO user_credentials (email, password, userid) VALUES (?, ?, ?) IF NOT EXISTS");

            // Insert the credentials info (this will return false if a user with that email address already exists)
            IStatement insertCredentialsStatement = preparedCredentials.Bind(request.Email, hashedPassword, request.UserId.ToGuid());
            RowSet credentialsResult = await _session.ExecuteAsync(insertCredentialsStatement).ConfigureAwait(false);

            // The first column in the row returned will be a boolean indicating whether the change was applied (TODO: Compensating action for user creation failure?)
            var applied = credentialsResult.Single().GetValue<bool>("[applied]");
            if (applied == false)
                throw new Exception("A user with that email address already exists");       // TODO: Right way to do error responses with grpc?

            PreparedStatement preparedUser = await _statementCache.NoContext.GetOrAddAsync(
                "INSERT INTO users (userid, firstname, lastname, email, created_date) VALUES (?, ?, ?, ?, ?) USING TIMESTAMP ?");

            // Insert the "profile" information using a parameterized CQL statement
            IStatement insertUserStatement =
                preparedUser.Bind(request.UserId.ToGuid(), request.FirstName, request.LastName, request.Email, timestamp, timestamp.ToMicrosecondsSinceEpoch());

            await _session.ExecuteAsync(insertUserStatement).ConfigureAwait(false);

            // Tell the world about the new user
            await _bus.Publish(new UserCreated
            {
                UserId = request.UserId,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                Timestamp = timestamp.ToTimestamp()
            }).ConfigureAwait(false);

            return new CreateUserResponse();
        }

        /// <summary>
        /// Verifies a user's credentials and returns the user's Id if successful, otherwise null.
        /// </summary>
        public async Task<VerifyCredentialsResponse> VerifyCredentials(VerifyCredentialsRequest request, ServerCallContext context)
        {
            // Lookup the user by email address
            IEnumerable<UserCredentials> results = await _userCredentialsTable.Where(uc => uc.EmailAddress == request.Email)
                                                                                       .ExecuteAsync().ConfigureAwait(false);
            
            var response = new VerifyCredentialsResponse();

            // Make sure we found a user
            UserCredentials credentials = results.SingleOrDefault();
            if (credentials == null)
                return response;

            // Verify the password hash
            if (PasswordHash.ValidatePassword(request.Password, credentials.Password) == false)
                return response;

            response.UserId = credentials.UserId.ToUuid();
            return response;
        }

        /// <summary>
        /// Gets multiple user profiles by their Ids.  
        /// </summary>
        public async Task<GetUserProfileResponse> GetUserProfile(GetUserProfileRequest request, ServerCallContext context)
        {
            var response = new GetUserProfileResponse();

            if (request.UserIds == null || request.UserIds.Count == 0)
                return response;

            // Since we're essentially doing a multi-get here, limit the number userIds (i.e. partition keys) to 20 in an attempt
            // to enforce some performance sanity.  Anything larger and we might want to consider a different data model that doesn't 
            // involve doing a multi-get
            if (request.UserIds.Count > 20) throw new ArgumentOutOfRangeException(nameof(request.UserIds), "Cannot do multi-get on more than 20 user id keys.");

            // Do some LINQ queries in parallel
            IEnumerable<Task<IEnumerable<LinqDtos.UserProfile>>> getProfilesTasks = request.UserIds.Select(uuid => _userProfileTable.Where(up => up.UserId == uuid.ToGuid()).ExecuteAsync());
            IEnumerable<LinqDtos.UserProfile>[] profiles = await Task.WhenAll(getProfilesTasks).ConfigureAwait(false);

            // Get first profile returned for each query if not null and add to response
            response.Profiles.Add(profiles.Select(ps => ps.SingleOrDefault()).Where(p => p != null).Select(p => new UserProfile
            {
                UserId = p.UserId.ToUuid(),
                Email = p.EmailAddress,
                FirstName = p.FirstName,
                LastName = p.LastName
            }));
            return response;
        }
    }
}