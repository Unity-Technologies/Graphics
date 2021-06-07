using System;

namespace UnityEditor.ShaderGraph.Internal
{
    // ------------------------------------------------------------------------------------------
    //
    //  The general use of precision follows this data flow
    //
    //  Precision -- user selectable precision setting on each node
    //       == apply precision inherit rules based on node inputs ==>
    //  GraphPrecision -- where "GraphPrecision.Graph" means use the graph default setting
    //       == fallback to graph defaults ==>
    //  GraphPrecision -- where "GraphPrecision.Graph" means it is switchable when in a subgraph
    //       == shadergraph concretization ==>
    //  ConcretePrecision -- the actual precision used by the node, half or single
    //
    //  We could at some point separate the two GraphPrecision uses into separate enums,
    //  but they're close enough we're using one enum for both uses at the moment
    //
    // ------------------------------------------------------------------------------------------

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
    // but before applying the fallback to the graph default
    // tracking this explicitly helps us build subgraph switchable precision behavior (any node using Graph can be switched)
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
