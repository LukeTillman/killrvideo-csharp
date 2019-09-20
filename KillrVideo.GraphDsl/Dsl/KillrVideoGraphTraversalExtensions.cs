using System.Collections.Generic;
using System.Linq;
using Gremlin.Net.Process.Traversal;
using Gremlin.Net.Structure;
using static Gremlin.Net.Process.Traversal.P;

using static KillrVideo.GraphDsl.__KillrVideo;

namespace KillrVideo.GraphDsl.Dsl {

    /// <summary>
    /// The KillrVideo Traversal class which exposes the available steps of the DSL.
    /// </summary>
    public static class KillrVideoGraphTraversalExtensions {
       
        /// <summary>
        /// Traverses from a "user" to an "videos" over the "rated" edge.
        /// </summary>
        public static GraphTraversal<Vertex, Vertex> RatedVideos(this GraphTraversal<Vertex, Vertex> t, int minRate)
        {
            return t.OutE(EdgeRated).Has(PropertyRating, Gt(minRate)).InV();
        }

        /// <summary>
        /// what other users rated those videos highly
        /// </summary>
        public static GraphTraversal<Vertex, Vertex> OtherUserRatedVideos(this GraphTraversal<Vertex, Vertex> t, int minRate, int sample)
        {                    
            return 
                // what other users rated those videos highly? (this is like saying "what users share my taste")    
                t.InE(EdgeRated).Has(PropertyRating, Gt(minRate))

                // but don't grab too many, or this won't work OLTP, and "by('rating')" favors the higher ratings
                 .Sample(sample).By(PropertyRating).OutV();
        }

        /// <summary>
        /// what other users rated those videos highly
        /// </summary>
        public static GraphTraversal<Vertex, Vertex> VideosRatedByOthers(this GraphTraversal<Vertex, Vertex> t, 
                                                                         int minRate, long minLocalRate) {
            return
                  // Now we're working with "similar users". For those users who share my taste, grab N highly rated 
                  // videos. Save the rating so we can sum the scores later, and use sack() because it does not require 
                  // path information. (as()/select() was slow)
                  t.Local<List<Vertex>>(
                        __.OutE(EdgeRated)
                            .Has(PropertyRating, Gt(minRate))
                            .Limit(minLocalRate))
                  .Sack(Operator.Assign)
                        .By(PropertyRating).InV();
        }

        /// <summary>
        /// what other users rated those videos highly
        /// </summary>
        public static GraphTraversal<Vertex, Vertex> AllRatedVideos(this GraphTraversal<Vertex, Vertex> t)
        {
            return t.Map<Vertex>(__.Out(EdgeRated).Dedup().Fold());
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
        /// This step is an alias for the <code>SideEffect()</code> step. As an alias, it makes certain aspects of the DSL more
        /// readable.
        /// </summary>
        public static GraphTraversal<S, E> Ensure<S, S1, E>(
            this GraphTraversal<S, E> t, GraphTraversal<S1, E> mutationTraversal) {
            return t.SideEffect(mutationTraversal);
        }
    }
}
