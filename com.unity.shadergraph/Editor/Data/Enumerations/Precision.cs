using System;

namespace UnityEditor.ShaderGraph.Internal
{
    // this is generally used for user-selectable precision
    [Serializable]
    enum Precision
    {
        Inherit,    // automatically choose the precision based on the inputs
        Single,     // force single precision (float)
        Half,       // force half precision
        Graph,      // use the graph default (for subgraphs this will properly switch based on the subgraph node setting)
    }

    // this is used when calculating precision within a graph
    // it basically represents the precision after applying the automatic inheritance rules
    // we do not yet resolve "Graph", as subgraphs may have switchable graph precision,
    // so we need to track that on every nodes
    public enum GraphPrecision
    {
        Single = 0,     // the ordering is different here so we can use the min function to resolve inherit/automatic behavior
        Graph = 1,
        Half = 2
    }

    // this is the actual set of precisions we have, a shadergraph must resolve every node to one of these
    // in subgraphs, this concrete precision is only used for preview, and may not represent the actual precision of those nodes
    // when used in a shader graph
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
