using System;

using System.Collections.Generic;
using Gremlin.Net.Process.Traversal;
using Gremlin.Net.Structure;

using static KillrVideo.SuggestedVideos.GraphDsl.Kv;

namespace KillrVideo.SuggestedVideos.GraphDsl
{

    /// <summary>
    /// The KillrVideo DSL TraversalSource which will provide the start steps for DSL-based traversals. 
    /// This TraversalSource spawns KillrVideoTraversal instances.
    /// </summary>
    public static class KillrVideoGraphTraversalSourceExtensions
    {
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

