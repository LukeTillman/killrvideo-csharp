using System;
using System.Collections.Generic;
using Gremlin.Net.Process.Traversal;
using Gremlin.Net.Structure;

using static Gremlin.Net.Process.Traversal.P;

using static KillrVideo.SuggestedVideos.GraphDsl.Kv;

namespace KillrVideo.SuggestedVideos.GraphDsl {
  
    /// <summary>
    /// Spawns anonymous traversal instances for the DSL.
    /// </summary>
    public static class __KillrVideo
    {
        /// <summary>
        /// Traverses from a "movie" to an "person" over the "actor" edge.
        /// </summary>
        public static GraphTraversal<object, Vertex> Actors()
        {
            return __.Out(EdgeActor).HasLabel(VertexPerson);
        }

        /// <summary>
        /// Gets or creates a "person".
        ///
        /// This step first checks for existence of a person given their identifier. If it exists then the person is
        /// returned and their "name" property updated. It is not possible to change the person's identifier once it is
        /// assigned (at least as defined by this DSL). If the person does not exist then a new person vertex is added
        /// with the specified identifier and name.
        /// </summary>
        public static GraphTraversal<object, Vertex> Person(string personId, string name)
        {
            if (string.IsNullOrEmpty(personId)) throw new ArgumentException("The personId must not be null or empty");
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("The name of the person must not be null or empty");

            return __.Coalesce<Vertex>(__.V().Has(VertexPerson, KeyPersonId, personId),
                                      __.AddV(VertexPerson).Property(KeyPersonId, personId)).
                   Property(KeyName, name);
        }

        /// <summary>
        ///  Assumes a "movie" vertex and traverses to a "genre" vertex with a filter on the name of the genre. This step is meant 
        /// to be used as part of a <code>filter()</code> step for movies.
        /// </summary>
        public static GraphTraversal<object, object> Genre(Genre genre, params Genre[] additionalGenres)
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
                return __.Out(EdgeBelongsTo).Map<object>(__.Identity()).Has(KeyName, genres[0]);
            else
                return __.Out(EdgeBelongsTo).Map<object>(__.Identity()).Has(KeyName, Within(genres));
        }

        /// <summary>
        /// Gets or creates a "actor".
        ///
        /// In this schema, an actor is a "person" vertex with an incoming "actor" edge from a "movie" vertex. This step
        /// therefore assumes that the incoming stream is a "movie" vertex and actors will be attached to that. This step
        /// checks for existence of the "actor" edge first before adding and if found will return the existing one. It
        /// further ensures the existence of the "person" vertex as provided by the <code>person()</code>.
        /// step.
        /// </summary>
        public static GraphTraversal<object, Vertex> Actor(string personId, string name)
        {
            // no validation here as it would just duplicate what is happening in person(). 
            //
            // as mentioned in the javadocs this step assumes an incoming "movie" vertex. it is immediately labelled as
            // "^movie". the addition of the caret prefix has no meaning except to provide for a unique labelling space
            // within the DSL itself.

            return __.As("^movie").
                     Coalesce<Vertex>(__KillrVideo.Actors().Has(VertexPerson, personId),
                                      __KillrVideo.Person(personId, name).AddE(EdgeActor).From("^movie").InV());
        }
    }
}
