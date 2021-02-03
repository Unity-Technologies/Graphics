using System;

namespace UnityEditor.ShaderGraph.Internal
{
    [Serializable]
    enum Precision
    {
        Inherit,    // automatically choose the precision based on the inputs
        Single,     // force single precision (float)
        Half,       // force half precision
        Graph,      // use the graph default (for subgraphs this will properly switch based on the subgraph node setting)
    }

    public enum GraphPrecision
    {
        Single = 0,     // ordered by priority
        Graph = 1,
        Half = 2
    }

    [Serializable]
    public enum ConcretePrecision
    {
        Single,
        Half,
    }

    // inherit(auto) rules for combining input types

    // half + half ==> half
    // single + single ==> single
    // single + half ==> single
    // single + graph ==> single
    // half + graph ==> graph
    // single + half + graph ==> single
    //
    // basically: take the min when arranged like so:   single(0), graph(1), half(2)
}
