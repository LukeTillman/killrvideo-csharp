using System;
using System.Collections.Generic;
using Gremlin.Net.Process.Traversal;
using Gremlin.Net.Structure;

using static Gremlin.Net.Process.Traversal.P;

namespace KillrVideo.GraphDsl {
  
    /// <summary>
    /// Spawns anonymous traversal instances for the DSL.
    /// </summary>
    public static class __KillrVideo {

        // Vertices labels
        public const String VertexUser           = "user";
        public const String VertexVideo          = "video";
        public const String VertexTag            = "tag";

        // Edges types
        public const String EdgeRated            = "rated";
        public const String EdgeUploaded         = "uploaded";
        public const String EdgeTagWith          = "taggedWith";

        // Properties (both Vertices and edges)
        public const String PropertyRating       = "rating";
        public const String PropertyVideoId      = "videoId";
        public const String PropertyUserId       = "userId";
        public const String PropertyName         = "name";
        public const String PropertyDescription  = "description";
        public const String PropertyAddedDate    = "added_date";
        public const String PropertyPreviewImage = "preview_image_location";
        public const String PropertyTaggedDate   = "tagged_date";
        public const String PropertyEmail        = "email";

        // Meta
        public const String KeyVertex            = "_vertex";
        public const String KeyInDegree          = "_inDegree";
        public const String KeyOutDegree         = "_outDegree";
        public const String KeyDegree            = "_degree";
        public const String KeyDistribution      = "_distribution";

        /// <summary>
        /// Take a deep breath here, you will be fine... hopefully 
        /// </summary>
        public static GraphTraversal<Vertex, IDictionary<string, Vertex>> RecommendVideos(this GraphTraversalSource g,
                                                                           string userId,
                                                                           int numberOfVideosExpected,
                                                                           int minimumRating,
                                                                           int numToSample,
                                                                           int minimumLocalRating) {
            GraphTraversal<Vertex, IDictionary<string, Vertex>> traversal =

              // Get current user from its ID
              g.V().HasLabel(VertexUser).Has(PropertyUserId, userId).As("^currentUser")

              // Find all related watched video as they rated
              .AllRatedVideos().As("^watchedVideos")

              // Users rating highly the same video as our user
              .Select<Vertex>("^currentUser")
              .RatedVideos(minimumRating)
              .OtherUserRatedVideos(minimumRating, numToSample)
              .Where(Neq("^currentUser"))

              // Video rated high by users previously
              .VideosRatedByOthers(minimumRating, minimumLocalRating)
              .Not(__.Where(Within("^watchedVideos")))

              // Filter out the video if for some reason there is no uploaded edge to a user
              // I found this could be a case where an "uploaded" edge was not created 
              // for a video given we don't guarantee graph data
              .Filter(__.In(EdgeUploaded).HasLabel(VertexUser))

              // what are the most popular videos as calculated by the sum of all their ratings
              .Group<string, long>()
              .By().By(__.Sack<object>()
              .Sum<long>())

              // now that we have that big map of [video: score], lets order it
              .Order(Scope.Local).By(Column.Values, Order.Decr)
              .Limit<IDictionary<Vertex, long>>(Scope.Local, numberOfVideosExpected)
              .Select<Vertex>(Column.Keys)
              .Unfold<Vertex>()
              .Project<Vertex>(VertexVideo, VertexUser)
              .By()
              .By(__.In(EdgeUploaded));
            return traversal;
        }

		/// <summary>
        /// Create VideoVertex
        /// </summary>
        public static GraphTraversal<Vertex, Vertex> CreateOrUpdateVideoVertex(this GraphTraversalSource g, 
                                                             String videoid, 
                                                             String videoName, 
                                                             String videoDescription,
                                                             String videoPreviewLocation) {
            return g.V().Has(VertexVideo, PropertyVideoId, videoid)
                .Fold()
                .Coalesce<Vertex>(
                 __.Unfold<Vertex>(),

                 //Add or update video
                 __.AddV(VertexVideo)
                        .Property(PropertyVideoId, videoid)
                        .Property(PropertyName, videoName)
                        .Property(PropertyDescription, videoDescription)
                        .Property(PropertyPreviewImage, videoPreviewLocation)
                        .Property(PropertyAddedDate, DateTimeOffset.UtcNow));
        }

        /// <summary>
        /// Create VideoVertex
        /// </summary>
        public static GraphTraversal<Vertex, Vertex> CreateEdgeUploadedForUserAndVideo(
                            this GraphTraversal<Vertex, Vertex> t, String userId)
        {
            return t.SideEffect(
                    __.As("^" + VertexVideo).Coalesce<Vertex>(
                        
                        __.In(EdgeUploaded)
                          .HasLabel(VertexUser)
                          .Has(PropertyUserId, userId),
                        
                        __.V()
                          .Has(VertexUser, PropertyUserId, userId)
                          .AddE(EdgeUploaded)
                          .To("^" + VertexVideo)
                         .InV()
                      )
                );
        }

        /// <summary>
        /// Create VideoVertex
        /// </summary>
        public static GraphTraversal<Vertex, Vertex> CreateTagVertexAndEdgeToVideo(
                             this GraphTraversal<Vertex, Vertex> t, String tag) {
                return t.SideEffect(
                    __.As("^" + VertexVideo).Coalesce<Vertex>(
                        
                       __.Out(EdgeTagWith)
                         .HasLabel(VertexTag)
                         .Has(PropertyName, tag),
                        
                       __.Coalesce<Vertex>(
                           __.V().Has(VertexTag,PropertyName, tag),

                           __.AddV(VertexTag)
                             .Property(PropertyName, tag)
                             .Property(PropertyTaggedDate, DateTimeOffset.UtcNow)
                         )
                        .AddE(EdgeTagWith)
                        .From("^" + VertexVideo).InV()
                     )
                );
        }

        /// <summary>
        /// Create User Vertex
        /// </summary>
        public static GraphTraversal<Vertex, Vertex> CreateUserVertex(
            this GraphTraversalSource g, String userId, String userEmail) {
            return g.V().AddV(VertexUser)
                    .Property(PropertyUserId, userId)
                    .Property(PropertyEmail, userEmail)
                    .Property(PropertyAddedDate, DateTimeOffset.UtcNow);
        }

        /// <summary>
        /// Create User Vertex
        /// </summary>
        public static GraphTraversal<Vertex, Vertex> UserRateVideo(
            this GraphTraversalSource g, String userId, String videoId, int rate) {
            return 
                // Locate Video and Set result as a variable named ^video 
                g.V().HasLabel(VertexVideo).Has(PropertyVideoId, videoId)
                     .SideEffect(
                       __.As("^" + VertexVideo)
                          // Locate target User vertex by its unique UserId         
                          // Create edge 'rated' from User to Video with value
                         .Coalesce<object>(
                             __.V().HasLabel(VertexUser)
                                   .Has(PropertyUserId, userId))
                         .AddE(EdgeRated).Property(PropertyRating, rate)
                      .To("^" + VertexVideo)
                      .InV()
                     );
        }
    }
}
