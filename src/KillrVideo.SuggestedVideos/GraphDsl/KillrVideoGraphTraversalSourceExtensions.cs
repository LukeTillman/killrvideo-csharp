using System;

using System.Collections.Generic;
using Gremlin.Net.Process.Traversal;
using Gremlin.Net.Structure;

using static KillrVideo.SuggestedVideos.GraphDsl.Kv;
using static Gremlin.Net.Process.Traversal.P;


namespace KillrVideo.SuggestedVideos.GraphDsl
{

     /// <summary>
    /// The KillrVideo DSL TraversalSource which will provide the start steps for DSL-based traversals. 
    /// This TraversalSource spawns KillrVideoTraversal instances.
    /// </summary>
    public static class KillrVideoGraphTraversalSourceExtensions
    {

        /// <summary>
        /// Take a deep breath here, you will be fine... hopefully 
        /// </summary>
        public static GraphTraversal<Vertex, IDictionary<string, Vertex>> recommendUserByRating(this GraphTraversalSource g, 
                                                                           string userId, 
                                                                           int numberOfVideosExpected,
                                                                           int minimumRating,
                                                                           int numToSample,
                                                                           int minimumLocalRating) {
            GraphTraversal < Vertex, IDictionary<string, Vertex>> traversal = g
              // Locate User by its userId
              // V().HasLabel("user").Has("userId", request.UserId.Value)
              .Users(userId)
              .As("^currentUser")

              // Find all related watched video as they rated
              .Map<Vertex>(__.Out("rated").Dedup().Fold())
              .As("^watchedVideos")

              // go back to our current user
              .Select<Vertex>("^currentUser")
              // for the video's I rated highly...
              .OutE("rated").Has("rating", Gt(minimumRating)).InV()
              // what other users rated those videos highly? (this is like saying "what users share my taste")
              .InE("rated").Has("rating", Gt(minimumRating))
              // but don't grab too many, or this won't work OLTP, and "by('rating')" favors the higher ratings
              .Sample(numToSample).By("rating").OutV()
              // (except me)
              .Where(Neq("^currentUser"))
              // Now we're working with "similar users". For those users who share my taste, grab N highly rated 
              // videos. Save the rating so we can sum the scores later, and use sack() because it does not require 
              // path information. (as()/select() was slow)
              .Local<List<Vertex>>(
                    __.OutE("rated")
                      .Has("rating", Gt(minimumRating))
                      .Limit(minimumLocalRating))
              .Sack(Operator.Assign)
              .By("rating").InV()

              // excluding the videos I have already watched
              .Not(__.Where(Within("^watchedVideos")))

              // Filter out the video if for some reason there is no uploaded edge to a user
              // I found this could be a case where an "uploaded" edge was not created for a video given we don't guarantee graph data
              .Filter(__.In("uploaded").HasLabel("user"))

              // what are the most popular videos as calculated by the sum of all their ratings
              .Group<string, long>()
              .By().By(__.Sack<object>()
              .Sum<long>())

              // now that we have that big map of [video: score], lets order it
              .Order(Scope.Local).By(Column.Values, Order.Decr)
              .Limit<IDictionary<Vertex, long>>(Scope.Local, numberOfVideosExpected)
              .Select<Vertex>(Column.Keys)
              .Unfold<Vertex>()
              .Project<Vertex>("video", "user")
              .By()
              .By(__.In("uploaded"));
            return traversal;
        }

        /// <summary>
        /// Gets movies by their title.
        /// </summary>
        public static GraphTraversal<Vertex, Vertex> Movies(this GraphTraversalSource g, string title, params string[] additionalTitles)
        {
            var titles = new List<string>();
            titles.Add(title);
            titles.AddRange(additionalTitles);
            GraphTraversal<Vertex, Vertex> t = g.V().HasLabel(VertexMovie);
            if (titles.Count == 1)
            {
                t = t.Has(KeyTitle, titles[0]);
            }
            else if (titles.Count > 1)
            {
                t = t.Has(KeyTitle, P.Within(titles.ToArray()));
            }
            return t;
        }

        /// <summary>
        /// Gets users by their identifier
        /// </summary>
        public static GraphTraversal<Vertex, Vertex> Users(this GraphTraversalSource g, string userId, params string[] additionalUserIds)
        {
            var userIds = new List<string>();
            userIds.Add(userId);
            userIds.AddRange(additionalUserIds);
            GraphTraversal<Vertex, Vertex> t = g.V().HasLabel(VertexUser);
            if (userIds.Count == 1)
            {
                t = t.Has(KeyUserId, userIds[0]);
            }
            else if (userIds.Count > 1)
            {
                t = t.Has(KeyUserId, P.Within(userIds.ToArray()));
            }
            return t;
        }

        /// <summary>
        /// Ensures that a "movie" exists. This step performs a number of validations on the various parameters passed to it and then 
        /// checks for existence of the movie based on the identifier for the movie. If it exists then the movie is returned with 
        /// its mutable properties updated (all are mutable except for the "movieId" as defined by this DSL). If it does not exist then
        /// a new "movie" vertex is added.
        /// </summary>
        public static GraphTraversal<Vertex, Vertex> Movie(this GraphTraversalSource g, string movieId, string title, int year, int duration,
                                                          string country = "", string production = "")
        {
            if (year < 1895) throw new ArgumentException("The year of the movie cannot be before 1895");
            if (year > DateTime.Now.Year) throw new ArgumentException("The year of the movie can not be in the future");
            if (duration <= 0) throw new ArgumentException("The duration of the movie must be greater than zero");
            if (string.IsNullOrEmpty(movieId)) throw new ArgumentException("The movieId must not be null or empty");
            if (string.IsNullOrEmpty(title)) throw new ArgumentException("The title of the movie must not be null or empty");

            // performs a "get or create/update" for the movie vertex. if it is present then it simply returns the existing
            // movie and updates the mutable property keys. if it is not, then it is created with the specified movieId
            // and properties.
            return g.V().
                Has(VertexMovie, KeyVideoId, movieId).
                Fold().
                Coalesce<Vertex>(__.Unfold<Vertex>(),
                                 __.AddV(VertexMovie).Property(KeyVideoId, movieId)).
                Property(KeyTitle, title).
                Property(KeyCountry, country).
                Property(KeyProduction, production).
                Property(KeyYear, year).
                Property(KeyDuration, duration);
        }
    }
}

