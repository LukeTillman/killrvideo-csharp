using System;
using System.Threading.Tasks;

using Dse;
using Dse.Graph;
using Gremlin.Net.Process.Traversal;
using Serilog;

using KillrVideo.MessageBus;
using KillrVideo.VideoCatalog.Events;
using DryIocAttributes;

using static KillrVideo.GraphDsl.__KillrVideo;

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
            await UpdatGraphForNewVideo(video);
        }

        /// <summary>
        /// Create new node in the Graph for
        /// </summary>
        public async Task UpdatGraphForNewVideo(YouTubeVideoAdded video) {
            
            String videoId = video.VideoId.ToGuid().ToString();
            String userId  = video.UserId.ToGuid().ToString();
            Logger.Information("{user} Inserting video {video} to graph", userId, videoId);

            // Create Traversal to add a Vertex Video, Edge 'upload from user to Vide, and Tags
            var traversal = DseGraph.Traversal(_session)

                // Creating Video Vertex based on ID
                .CreateOrUpdateVideoVertex(
                    videoId,
                    video.Name,
                    video.Description,
                    video.PreviewImageLocation
                )

                // Edge User -- Uploaded --> Video
                .CreateEdgeUploadedForUserAndVideo(userId);
            
                // Add Tags Vertices and edges to video
                foreach(string tag in video.Tags) {
                  traversal.CreateTagVertexAndEdgeToVideo(tag);
                }

             await _session.ExecuteGraphAsync(traversal);
        }
    }
}
