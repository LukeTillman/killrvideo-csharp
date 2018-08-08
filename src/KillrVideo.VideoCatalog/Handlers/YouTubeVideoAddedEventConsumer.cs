using System;
using System.Threading.Tasks;

using Dse;
using Dse.Graph;
using Gremlin.Net.Process.Traversal;
using Serilog;

using KillrVideo.MessageBus;
using KillrVideo.VideoCatalog.Events;
using DryIocAttributes;

namespace KillrVideo.VideoCatalog.Handlers
{
    /// <summary>
    /// Consume video creation and update Graph Accordingly
    /// </summary>
    [ExportMany, Reuse(ReuseType.Transient)]
    public class YouTubeVideoAddedEventConsumer : IHandleMessage<YouTubeVideoAdded> {
        
        /// <summary>
        /// Logger for the class to write into both files and console
        /// </summary>
        private static readonly ILogger Logger = Log.ForContext(typeof(YouTubeVideoAddedEventConsumer));

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
        public YouTubeVideoAddedEventConsumer(IDseSession session,  IBus bus)  {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (bus == null)     throw new ArgumentNullException(nameof(bus));
            _session = session;
            _bus     = bus;
        }

        /// <summary>
        /// Subscription to bus for message YouTubeVideoAdded
        /// </summary>
        public async Task Handle(YouTubeVideoAdded video) {
            Logger.Information("Incoming Event 'YouTubeVideoAdded' {videoid}", video.VideoId.ToGuid());
            await AddVideoVertexToGraph(video);
        }

        /// <summary>
        /// Create new node in the Graph for
        /// </summary>
        public async Task AddVideoVertexToGraph(YouTubeVideoAdded video) {
            Logger.Information("Inserting to graph video {video} ", video.VideoId.ToGuid());

            // Create Traversal
            GraphTraversalSource g = DseGraph.Traversal(_session);
            // Add Vertex 'video' with expected properties asynchronously
            await _session.ExecuteGraphAsync(
                g.V().AddV("video")
                     .Property("videoId", video.VideoId.ToGuid().ToString())
                     .Property("name", video.Name)
                     .Property("description", video.Description)
                     .Property("preview_image_location", video.PreviewImageLocation)
                     .Property("added_date", DateTimeOffset.UtcNow));
        }
    }
}
