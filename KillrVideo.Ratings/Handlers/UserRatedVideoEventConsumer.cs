using System;
using System.Threading.Tasks;
using DryIocAttributes;
using Dse;
using Dse.Graph;
using KillrVideo.MessageBus;
using KillrVideo.Ratings.Events;
using Serilog;

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
            await RateVideoInGraph(message.VideoId.ToGuid().ToString(),
                             message.UserId.ToGuid().ToString(),
                             message.Rating);
        }

        public async Task RateVideoInGraph(String videoId, String userId, int rating) {
            var traversal = DseGraph.Traversal(_session).V(); //Added V() here so it doesn't fail
                            //TODO Not sure what is expected here
                                   // .UserRateVideo(userId, videoId, rating);
            await _session.ExecuteGraphAsync(traversal);
        }
    }
}
