using System.Linq;
using System.Collections.Generic;
using Gremlin.Net.Process.Traversal;

using static KillrVideo.GraphDsl.__KillrVideo;

namespace KillrVideo.GraphDsl
{
    /// <summary>
    /// Provides for pre-built data enrichment options for the <code>enrich(Enrichment...)</code> step. These options will
    /// include extra information about the <code>Vertex</code>> when output from that step. Note that the enrichment 
    /// examples presented here are examples to demonstrate this concept. The primary lesson here is to show how one might 
    /// merge map results as part of a DSL. These enrichment options may not be suitable for traversals in production systems 
    /// as counting all edges might add an unreasonable amount of time to an otherwise fast traversal.
    /// </summary>
    public class Enrichment
    {
        private List<string> projectedKeys;

        private List<GraphTraversal<object, object>> traversals;

        private Enrichment(string projectedKeys, GraphTraversal<object, object> traversal) :
            this(new List<string>() { projectedKeys }, new List<GraphTraversal<object, object>>() { traversal })
        {
        }

        private Enrichment(List<string> projectedKeys, List<GraphTraversal<object, object>> traversals)
        {
            this.projectedKeys = projectedKeys;
            this.traversals = traversals;
        }

        public List<GraphTraversal<object, object>> GetTraversals()
        {
            return traversals;
        }

        public List<string> GetProjectedKeys()
        {
            return projectedKeys;
        }

        /// <summary>
        /// Include the <code>Vertex</code> itself as a value in the enriched output which might be helpful if additional
        /// traversing on that element is required.
        /// </summary>
        public static Enrichment Vertex()
        {
            return new Enrichment(KeyVertex, __.Identity());
        }

        /// <summary>
        /// The number of incoming edges on the <code>Vertex</code>.
        /// </summary>
        public static Enrichment InDegree()
        {
            return new Enrichment(KeyInDegree, __.Map<object>(__.InE().Count()));
        }


        /// <summary>
        /// The number of outgoing edges on the <code>Vertex</code>.
        /// </summary>
        public static Enrichment OutDegree()
        {
            return new Enrichment(KeyOutDegree, __.Map<object>(__.OutE().Count()));
        }

        /// <summary>
        /// The total number of in and out edges on the <code>Vertex</code>.
        /// </summary>
        public static Enrichment Degree()
        {
            return new Enrichment(KeyDegree, __.Map<object>(__.BothE().Count()));
        }

        /// <summary>
        /// Calculates the edge label distribution for the <code>Vertex</code>>.
        /// </summary>
        public static Enrichment Distribution()
        {
            return new Enrichment(KeyDistribution, __.Map<object>(__.BothE().GroupCount<string>().By(T.Label)));
        }

        /// <summary>
        /// Chooses the keys to include in the output.
        /// </summary>
        public static Enrichment Keys(params string[] keys)
        {
            var valueTraversals = keys.Select(k => __.Values<object>(k)).ToList();
            return new Enrichment(keys.ToList(), valueTraversals);
        }
    }
}

