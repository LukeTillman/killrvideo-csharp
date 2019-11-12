using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using Dse;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using KillrVideo.Cassandra;
using KillrVideo.MessageBus;
using KillrVideo.Protobuf;
using KillrVideo.Protobuf.Services;
using KillrVideo.UserManagement.Events;

namespace KillrVideo.UserManagement
{
    /// <summary>
    /// An implementation of the user management service that stores accounts in Cassandra and publishes events on a message bus.
    /// </summary>
    [Export(typeof(IGrpcServerService))]
    public class UserManagementServiceImpl : UserManagementService.UserManagementServiceBase, IConditionalGrpcServerService
    {
        private readonly IDseSession _session;
        private readonly IBus _bus;
        private readonly UserManagementOptions _options;
        private readonly PreparedStatementCache _statementCache;

        public ServiceDescriptor Descriptor => UserManagementService.Descriptor;

        public UserManagementServiceImpl(IDseSession session, PreparedStatementCache statementCache, IBus bus, UserManagementOptions options)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (statementCache == null) throw new ArgumentNullException(nameof(statementCache));
            if (bus == null) throw new ArgumentNullException(nameof(bus));
            if (options == null) throw new ArgumentNullException(nameof(options));
            _session = session;
            _statementCache = statementCache;
            _bus = bus;
            _options = options;
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
        public bool ShouldRun()
        {
            // Use this implementation when LINQ is not enabled or not present in the host config
            return _options.LinqEnabled == false;
        }

        /// <summary>
        /// Creates a new user account.
        /// </summary>
        public override async Task<CreateUserResponse> CreateUser(CreateUserRequest request, ServerCallContext context)
        {
            // Hash the user's password
            string hashedPassword = PasswordHash.CreateHash(request.Password);

            DateTimeOffset timestamp = DateTimeOffset.UtcNow;

            PreparedStatement preparedCredentials = await _statementCache.GetOrAddAsync(
                "INSERT INTO user_credentials (email, password, userid) VALUES (?, ?, ?) IF NOT EXISTS");

            // Insert the credentials info (this will return false if a user with that email address already exists)
            IStatement insertCredentialsStatement = preparedCredentials.Bind(request.Email, hashedPassword, request.UserId.ToGuid());
            RowSet credentialsResult = await _session.ExecuteAsync(insertCredentialsStatement).ConfigureAwait(false);

            // The first column in the row returned will be a boolean indicating whether the change was applied (TODO: Compensating action for user creation failure?)
            var applied = credentialsResult.Single().GetValue<bool>("[applied]");
            if (applied == false)
            {
                var status = new Status(StatusCode.AlreadyExists, "A user with that email address already exists");
                throw new RpcException(status);
            }

            PreparedStatement preparedUser = await _statementCache.GetOrAddAsync(
                "INSERT INTO users (userid, firstname, lastname, email, created_date) VALUES (?, ?, ?, ?, ?)");

            // Insert the "profile" information using a parameterized CQL statement
            IStatement insertUserStatement = preparedUser.Bind(request.UserId.ToGuid(), request.FirstName, request.LastName, request.Email, timestamp)
                                                         .SetTimestamp(timestamp);

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
        public override async Task<VerifyCredentialsResponse> VerifyCredentials(VerifyCredentialsRequest request, ServerCallContext context)
        {
            PreparedStatement preparedStatement =
                await _statementCache.GetOrAddAsync("SELECT email, password, userid FROM user_credentials WHERE email = ?");

            // Use the get credentials prepared statement to find credentials for the user
            RowSet result = await _session.ExecuteAsync(preparedStatement.Bind(request.Email)).ConfigureAwait(false);
            
            // We should get a single credentials result or no results
            Row row = result.SingleOrDefault();
            if (row == null || PasswordHash.ValidatePassword(request.Password, row.GetValue<string>("password")) == false)
            {
                var status = new Status(StatusCode.Unauthenticated, "Email address or password are not correct.");
                throw new RpcException(status);
            }
            
            return new VerifyCredentialsResponse { UserId = row.GetValue<Guid>("userid").ToUuid() };
        }
        
        /// <summary>
        /// Gets multiple user profiles by their Ids.  
        /// </summary>
        public override async Task<GetUserProfileResponse> GetUserProfile(GetUserProfileRequest request, ServerCallContext context)
        {
            var response = new GetUserProfileResponse();

            if (request.UserIds == null || request.UserIds.Count == 0)
                return response;

            // Since we're essentially doing a multi-get here, limit the number userIds (i.e. partition keys) to 20 in an attempt
            // to enforce some performance sanity.  Anything larger and we might want to consider a different data model that doesn't 
            // involve doing a multi-get
            if (request.UserIds.Count > 20)
            {
                var status = new Status(StatusCode.InvalidArgument, "Cannot get more than 20 user profiles at once");
                throw new RpcException(status);
            }

            // As an example, we'll do the multi-get at the CQL level using an IN() clause (i.e. let Cassandra handle it).  For an example of
            // doing it at the driver level, see the VideoCatalog's GetVideoPreviews method

            // Build a parameterized CQL statement with an IN clause
            var parameterList = string.Join(", ", Enumerable.Repeat("?", request.UserIds.Count));
            var cql = $"SELECT userid, firstname, lastname, email FROM users WHERE userid IN ({parameterList})";
            var statement = new SimpleStatement(cql, request.UserIds.Select(uuid => uuid.ToGuid()).Cast<object>().ToArray());
            
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