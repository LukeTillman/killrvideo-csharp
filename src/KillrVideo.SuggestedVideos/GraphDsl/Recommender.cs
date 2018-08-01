using System;
using System.Collections.Generic;
using Gremlin.Net.Process.Traversal;
using Gremlin.Net.Structure;

using static KillrVideo.SuggestedVideos.GraphDsl.Kv;

namespace KillrVideo.SuggestedVideos.GraphDsl
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
            {Recommender.SmallSample, __.OutE(EdgeActor).Sample(3).InV().Fold()},
            {Recommender.LargeSample, __.OutE(EdgeActor).Sample(10).InV().Fold()},
            {Recommender.Fifty50Sample, __.OutE(EdgeActor).Coin(0.5).InV().Fold()},
            {Recommender.TimedSample, __.OutE(EdgeActor).TimeLimit(250).InV().Fold()},
            {Recommender.All, __.OutE(EdgeActor).InV().Fold()}
        };
    }
}