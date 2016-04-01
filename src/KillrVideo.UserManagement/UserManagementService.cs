using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using Cassandra;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using KillrVideo.Cassandra;
using KillrVideo.Host.Config;
using KillrVideo.MessageBus;
using KillrVideo.Protobuf;
using KillrVideo.UserManagement.Events;

namespace KillrVideo.UserManagement
{
    /// <summary>
    /// An implementation of the user management service that stores accounts in Cassandra and publishes events on a message bus.
    /// </summary>
    [Export(typeof(IGrpcServerService))]
    public class UserManagementServiceImpl : UserManagementService.IUserManagementService, IConditionalGrpcServerService
    {
        private readonly ISession _session;
        private readonly IBus _bus;
        private readonly PreparedStatementCache _statementCache;

        public UserManagementServiceImpl(ISession session, PreparedStatementCache statementCache, IBus bus)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (statementCache == null) throw new ArgumentNullException(nameof(statementCache));
            if (bus == null) throw new ArgumentNullException(nameof(bus));
            _session = session;
            _statementCache = statementCache;
            _bus = bus;
        }

        /// <summary>
        /// Convert this instance to a ServerServiceDefinition that can be run on a Grpc server.
        /// </summary>
        public ServerServiceDefinition ToServerServiceDefinition()
        {
            return UserManagementService.BindService(this);
        }

        /// <summary>
        /// Returns true if this service should run given the configuration of the host.
        /// </summary>
        public bool ShouldRun(IHostConfiguration hostConfig)
        {
            // Use this implementation when LINQ is not enabled or not present in the host config
            return UserManagementConfig.UseLinq(hostConfig) == false;
        }

        /// <summary>
        /// Creates a new user account.
        /// </summary>
        public async Task<CreateUserResponse> CreateUser(CreateUserRequest request, ServerCallContext context)
        {
            // Hash the user's password
            string hashedPassword = PasswordHash.CreateHash(request.Password);

            DateTimeOffset timestamp = DateTimeOffset.UtcNow;

            PreparedStatement preparedCredentials = await _statementCache.GetOrAddAsync(
                "INSERT INTO user_credentials (email, password, userid) VALUES (?, ?, ?) IF NOT EXISTS USING TIMESTAMP ?");

            // Insert the credentials info (this will return false if a user with that email address already exists)
            IStatement insertCredentialsStatement = preparedCredentials.Bind(request.Email, hashedPassword, request.UserId.ToGuid(),
                                                                             timestamp.ToMicrosecondsSinceEpoch());
            RowSet credentialsResult = await _session.ExecuteAsync(insertCredentialsStatement).ConfigureAwait(false);

            // The first column in the row returned will be a boolean indicating whether the change was applied (TODO: Compensating action for user creation failure?)
            var applied = credentialsResult.Single().GetValue<bool>("[applied]");
            if (applied == false)
                throw new Exception("A user with that email address already exists"); // TODO: Figure out how to do errors properly in grpc

            PreparedStatement preparedUser = await _statementCache.GetOrAddAsync(
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
            PreparedStatement preparedStatement =
                await _statementCache.GetOrAddAsync("SELECT email, password, userid FROM user_credentials WHERE email = ?");

            // Use the get credentials prepared statement to find credentials for the user
            RowSet result = await _session.ExecuteAsync(preparedStatement.Bind(request.Email)).ConfigureAwait(false);
            var response = new VerifyCredentialsResponse();
            
            // We should get a single credentials result or no results
            Row row = result.SingleOrDefault();
            if (row == null)
                return response;

            // Verify the password hash
            if (PasswordHash.ValidatePassword(request.Password, row.GetValue<string>("password")) == false)
                return response;

            response.UserId = row.GetValue<Guid>("userid").ToUuid();
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

            // As an example, we'll do the multi-get at the CQL level using an IN() clause (i.e. let Cassandra handle it).  For an example of
            // doing it at the driver level, see the VideoCatalog's GetVideoPreviews method

            // Build a parameterized CQL statement with an IN clause
            var parameterList = string.Join(", ", Enumerable.Repeat("?", request.UserIds.Count));
            var cql = $"SELECT userid, firstname, lastname, email FROM users WHERE userid IN ({parameterList})";
            var statement = new SimpleStatement(cql, request.UserIds.Cast<object>().ToArray());
            
            // Execute and map to UserProfile object
            RowSet resultRows = await _session.ExecuteAsync(statement).ConfigureAwait(false);
            response.Profiles.Add(resultRows.Select(MapRowToUserProfile));
            return response;
        }

        private static UserProfile MapRowToUserProfile(Row row)
        {
            return new UserProfile
            {
                UserId = row.GetValue<Guid>("userid").ToUuid(),
                FirstName = row.GetValue<string>("firstname"),
                LastName = row.GetValue<string>("lastname"),
                Email = row.GetValue<string>("email")
            };
        }
    }
}