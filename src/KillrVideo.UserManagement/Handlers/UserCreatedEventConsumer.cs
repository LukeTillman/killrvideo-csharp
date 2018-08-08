using System.ComponentModel.Composition;
using Dse;
using Dse.Graph;
using Gremlin.Net.Process.Traversal;
using Serilog;
using KillrVideo.MessageBus;
using System;
using System.Threading.Tasks;
using KillrVideo.UserManagement.Events;
using DryIocAttributes;

namespace KillrVideo.UserManagement.Handlers
{
    /// <summary>
    /// Consume video creation and update Graph Accordingly
    /// </summary>
    [ExportMany, Reuse(ReuseType.Transient)]
    public class UserCreatedEventConsumer : IHandleMessage<UserCreated> {
        
        /// <summary>
        /// Logger for the class to write into both files and console
        /// </summary>
        private static readonly ILogger Logger = Log.ForContext(typeof(UserCreatedEventConsumer));

        /// <summary>
        /// Inject Dse Session.
        /// </summary>
        private readonly IDseSession _session;

        /// <summary>
        /// Exchange messages between services.
        /// </summary>
        private readonly IBus _bus;

        /// <summary>
        /// Constructor with proper injection
        /// </summary>
        public UserCreatedEventConsumer(IDseSession session,  IBus bus)  {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (bus == null)     throw new ArgumentNullException(nameof(bus));
            _session = session;
            _bus     = bus;
        }

        /// <summary>
        /// Create a video in the Graph
        /// </summary>
        public async Task Handle(UserCreated user) {
            Logger.Information("Incoming Event 'UserCreated' user {userid}", user.UserId.ToGuid());
            await AddUserVertexToGraph(user);
        }

        /// <summary>
        /// Create new node in the Graph for
        /// </summary>
        public async Task AddUserVertexToGraph(UserCreated user) {
            Logger.Information("Inserting to graph Vertext user {user} ", user.UserId.ToGuid());

            // Create Traversal
            GraphTraversalSource g = DseGraph.Traversal(_session);
            // Add Vertex 'user' with expected properties asynchronously
            await _session.ExecuteGraphAsync(
                g.V().AddV("user")
                     .Property("userId", user.UserId.ToGuid().ToString())
                     .Property("email", user.Email)
                     .Property("added_date", DateTimeOffset.UtcNow));
        }
    }
}
