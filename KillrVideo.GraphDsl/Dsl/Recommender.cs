using System.Collections.Generic;
using Gremlin.Net.Process.Traversal;
using Gremlin.Net.Structure;


namespace KillrVideo.GraphDsl.Dsl
{

    /// <summary>
    /// Provides for pre-built "sampling" settings to the <code>recommend(int, int, Recommender, Traversal)<code>
    /// step. The sampling options help determine the nature of the initial set of movies to recommend, by limiting the
    /// number of actors used from highly rated movies of the user who is target for the recommendation.
    /// </summary>
    public enum Recommender
    {
        SmallSample,
        LargeSample,
        Fifty50Sample,
        TimedSample,
        All
    }

    public static class RecommenderLookup
    {
        public static readonly Dictionary<Recommender, GraphTraversal<object, IList<Vertex>>> Traversals = new Dictionary<Recommender, GraphTraversal<object, IList<Vertex>>>
        {
            {Recommender.SmallSample, __.OutE(KvGraph.EdgeActor).Sample(3).InV().Fold()},
            {Recommender.LargeSample, __.OutE(KvGraph.EdgeActor).Sample(10).InV().Fold()},
            {Recommender.Fifty50Sample, __.OutE(KvGraph.EdgeActor).Coin(0.5).InV().Fold()},
            {Recommender.TimedSample, __.OutE(KvGraph.EdgeActor).TimeLimit(250).InV().Fold()},
            {Recommender.All, __.OutE(KvGraph.EdgeActor).InV().Fold()}
        };
    }
}