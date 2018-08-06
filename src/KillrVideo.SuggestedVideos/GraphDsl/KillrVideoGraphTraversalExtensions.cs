using System;

using System.Linq;
using System.Collections.Generic;
using Gremlin.Net.Process.Traversal;
using Gremlin.Net.Structure;

using static Gremlin.Net.Process.Traversal.P;
using static Gremlin.Net.Process.Traversal.Scope;
using static Gremlin.Net.Process.Traversal.Column;
using static Gremlin.Net.Process.Traversal.Order;

using static KillrVideo.SuggestedVideos.GraphDsl.Kv;

namespace KillrVideo.SuggestedVideos.GraphDsl
{

    /// <summary>
    /// The KillrVideo Traversal class which exposes the available steps of the DSL.
    /// </summary>
    public static class KillrVideoGraphTraversalExtensions
    {
        /// <summary>
        /// Traverses from a "movie" to an "person" over the "actor" edge.
        /// </summary>
        public static GraphTraversal<Vertex, Vertex> Actors(this GraphTraversal<Vertex, Vertex> t)
        {
            return t.Out(EdgeActor).HasLabel(VertexPerson);
        }

        /// <summary>
        /// Traverses from a "movie" to a "rated" edge.
        /// </summary>
        public static GraphTraversal<Vertex, Edge> Ratings(this GraphTraversal<Vertex, Vertex> t)
        {
            return t.InE(EdgeRated);
        }

        /// <summary>
        /// Calls <code>rated(int, int)</code> with both arguments as zero.
        /// </summary>
        public static GraphTraversal<Vertex, Vertex> Rated(this GraphTraversal<Vertex, Vertex> t)
        {
            return Rated(t, 0, 0);
        }

        /// <summary>
        /// Traverses from a "user" to a "movie" over the "rated" edge, filtering those edges as specified. If both 
        /// arguments are zero then there is no rating filter.
        /// </summary>
        public static GraphTraversal<Vertex, Vertex> Rated(this GraphTraversal<Vertex, Vertex> t, int min, int max)
        {
            if (min < 0 || max > 10) throw new ArgumentException("min must be a value between 0 and 10");
            if (max < 0 || max > 10) throw new ArgumentException("min must be a value between 0 and 10");
            if (min != 0 && max != 0 && min > max) throw new ArgumentException("min cannot be greater than max");

            if (min == 0 && max == 0)
                return t.Out(EdgeRated);
            else if (min == 0)
                return t.OutE(EdgeRated).Has(KeyRating, Gt(min)).InV();
            else if (max == 0)
                return t.OutE(EdgeRated).Has(KeyRating, Lt(min)).InV();
            else
                return t.OutE(EdgeRated).Has(KeyRating, Between(min, max)).InV();
        }

        /// <summary>
        ///  Assumes a "movie" vertex and traverses to a "genre" vertex with a filter on the name of the genre. This step is meant 
        /// to be used as part of a <code>filter()</code> step for movies.
        /// </summary>
        public static GraphTraversal<Vertex, Vertex> Genre(this GraphTraversal<Vertex, Vertex> t, Genre genre, params Genre[] additionalGenres)
        {
            var genres = new List<string>();
            genres.Add(GenreLookup.Names[genre]);
            foreach (Genre current in additionalGenres)
            {
                genres.Add(GenreLookup.Names[current]);
            }

            if (genres.Count < 1)
                throw new ArgumentException("There must be at least one genre option provided");

            if (genres.Count == 1)
                return t.Out(EdgeBelongsTo).Has(KeyName, genres[0]);
            else
                return t.Out(EdgeBelongsTo).Has(KeyName, Within(genres));
        }

        /// <summary>
        /// Calls <code>rated(int, int)</code> with both arguments as zero.
        /// </summary>
        public static GraphTraversal<Vertex, IDictionary<string, long>> DistributionForAges(this GraphTraversal<Vertex, Edge> t, int start, int end)
        {
            if (start < 18) throw new ArgumentException("Age must be 18 or older");
            if (start > end) throw new ArgumentException("Start age must be greater than end age");
            if (end > 120) throw new ArgumentException("Now you're just being crazy");

            return t.Filter(__.OutV().Has(KeyAge, Between(start, end))).Group<string, long>().By(KeyRating).By(__.Count());
        }

        /// <summary>
        /// A convenience overload for <code>recommend(int, int, Recommender, Traversal)</code>.
        /// </summary>
        public static GraphTraversal<Vertex, Vertex> Recommend(this GraphTraversal<Vertex, Vertex> t, int recommendations, int minRating)
        {
            return Recommend(t, recommendations, minRating, Recommender.SmallSample, __.Identity());
        }

        /// <summary>
        /// A convenience overload for <code>recommend(int, int, Recommender, Traversal)</code>.
        /// </summary>
        public static GraphTraversal<Vertex, Vertex> Recommend(this GraphTraversal<Vertex, Vertex> t, int recommendations,
                                                                int minRating, GraphTraversal<object, object> include)
        {
            return Recommend(t, recommendations, minRating, Recommender.SmallSample, include);
        }

        /// <summary>
        /// A simple recommendation algorithm that starts from a "user" and examines movies the user has seen filtered by
        /// the {@code minRating} which removes movies that hold a rating lower than the value specified. 
        /// 
        /// It then samples the actors in the movies the user has seen and uses that to find other movies those actors have been in that
        /// the user has not yet seen. Those movies are grouped, counted and sorted based on that count to produce the recommendation.
        /// </summary>
        public static GraphTraversal<Vertex, Vertex> Recommend(
            this GraphTraversal<Vertex, Vertex> t,
            int recommendations,
            int minRating,
            Recommender recommender,
            GraphTraversal<object, object> include)
        {

            if (recommendations <= 0) throw new ArgumentException("recommendations must be greater than zero");

            return t.Rated(minRating, 0).
                     Aggregate("seen").
                     Local<List<Vertex>>(RecommenderLookup.Traversals[recommender]).
                     Unfold<Vertex>().In(EdgeActor).
                     Where(Without(new List<string> { "seen" })).
                     Where(include).
                     GroupCount<Vertex>().
                     Order(Local).
                     By(Values, Decr).
                     Limit<IDictionary<Vertex, long>>(Local, recommendations).
                     Select<Vertex>(Kv.KeyVideoId).
                     Unfold<Vertex>();
        }

        /// <summary>
        /// Expects an incoming <code>Vertex</code> and projects it to a <code>Map</code> with the specified <code>Enrichment</code>>
        /// values passed to it.
        /// </summary>
        public static GraphTraversal<Vertex, IDictionary<string, object>> Enrich(this GraphTraversal<Vertex, Vertex> t, bool includeIdLabel, params Enrichment[] enrichments)
        {
            var projectTraversals = enrichments.Select(e => e.GetTraversals()).SelectMany(i => i).ToList();
            if (includeIdLabel)
            {
                projectTraversals.Add(__.Id());
                projectTraversals.Add(__.Map<object>(__.Label()));
            }

            var keys = enrichments.Select(e => e.GetProjectedKeys()).SelectMany(i => i).ToList();
            if (includeIdLabel)
            {
                keys.Add("id");
                keys.Add("label");
            }

            var projectedKeys = keys.GetRange(1, keys.Count() - 1).ToArray();
            var te = t.Project<object>(keys.First(), projectedKeys);
            foreach (GraphTraversal<object, object> projectTraversal in projectTraversals)
            {
                te = te.By(projectTraversal);
            }

            return te;
        }

        /// <summary>
        /// Gets or creates a "person".
        ///
        /// This step first checks for existence of a person given their identifier. If it exists then the person is
        /// returned and their "name" property updated. It is not possible to change the person's identifier once it is
        /// assigned (at least as defined by this DSL). If the person does not exist then a new person vertex is added
        /// with the specified identifier and name.
        /// </summary>
        public static GraphTraversal<S, Vertex> Person<S, E>(this GraphTraversal<S, E> t, string personId, string name)
        {
            if (string.IsNullOrEmpty(personId)) throw new ArgumentException("The personId must not be null or empty");
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("The name of the person must not be null or empty");

            return t.Coalesce<Vertex>(__.V().Has(VertexPerson, KeyPersonId, personId),
                                      __.AddV(VertexPerson).Property(KeyPersonId, personId)).
                     Property(KeyName, name);
        }

        /// <summary>
        /// Gets or creates a "actor".
        ///
        /// In this schema, an actor is a "person" vertex with an incoming "actor" edge from a "movie" vertex. This step
        /// therefore assumes that the incoming stream is a "movie" vertex and actors will be attached to that. This step
        /// checks for existence of the "actor" edge first before adding and if found will return the existing one. It
        /// further ensures the existence of the "person" vertex as provided by the {@link #person(String, String)}
        /// step.
        /// </summary>
        public static GraphTraversal<S, Vertex> Actor<S, E>(this GraphTraversal<S, E> t, string personId, string name)
        {
            // no validation here as it would just duplicate what is happening in person(). 
            //
            // as mentioned in the javadocs this step assumes an incoming "movie" vertex. it is immediately labelled as
            // "^movie". the addition of the caret prefix has no meaning except to provide for a unique labelling space
            // within the DSL itself.

            return t.As("^movie").
                     Coalesce<Vertex>(__KillrVideo.Actors().Has(VertexPerson, personId),
                                      __KillrVideo.Person(personId, name).AddE(EdgeActor).From("^movie").InV());
        }

        /// <summary>
        /// This step is an alias for the <code>SideEffect()</code> step. As an alias, it makes certain aspects of the DSL more
        /// readable.
        /// </summary>
        public static GraphTraversal<S, E> Ensure<S, S1, E>(this GraphTraversal<S, E> t, GraphTraversal<S1, E> mutationTraversal)
        {
            return t.SideEffect(mutationTraversal);
        }
    }
}
