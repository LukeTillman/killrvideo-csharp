using System;
using System.Threading.Tasks;

using Dse;
using Dse.Graph;
using Gremlin.Net.Process.Traversal;
using Serilog;

using KillrVideo.MessageBus;
using KillrVideo.Ratings.Events;
using DryIocAttributes;

namespace KillrVideo.Ratings
{
    /// <summary>
    /// Consume rating creation and update Graph Accordingly
    /// </summary>
    [ExportMany, Reuse(ReuseType.Transient)]
    public class UserRatedVideoEventConsumer : IHandleMessage<UserRatedVideo> {
        
        /// <summary>
        /// Logger for the class to write into both files and console
        /// </summary>
        private static readonly ILogger Logger = Log.ForContext(typeof(UserRatedVideoEventConsumer));

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
        public UserRatedVideoEventConsumer(IDseSession session,  IBus bus)  {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (bus == null)     throw new ArgumentNullException(nameof(bus));
            _session = session;
            _bus = bus;
        }

        /// <summary>
        /// Create a video in the Graph
        /// </summary>
        public async Task Handle(UserRatedVideo message) {
            Logger.Information("Incoming Event 'UserRatedVideo' rated video {videoid}: ", message.VideoId.ToGuid());
            await RateVideoInGraph(message.VideoId.ToGuid(),
                             message.UserId.ToGuid(),
                             message.Rating);
        }

        public async Task RateVideoInGraph(Guid videoId, Guid userId, int rating) {
            GraphTraversalSource g = DseGraph.Traversal(_session);
            await _session.ExecuteGraphAsync(
                    // Begin of Traversal
                    g.V()
                    // Locate target Video vertex by its unique videoId
                    .HasLabel("video").Has("videoId", videoId.ToString())
                    // Set result as a variable named ^video 
                    .SideEffect(__.As("^video")
                       // Keep going for request 
                       .Coalesce<object>(
                          // Locate target User vertex by its unique UserId         
                          __.V()
                            .HasLabel("user")
                            .Has("userId", userId.ToString())
                          // Create edge 'rated' from User to Video with value
                            .AddE("rated").Property("rating", rating)
                            .To("^video").InV()))
            );
        }
    }
}
