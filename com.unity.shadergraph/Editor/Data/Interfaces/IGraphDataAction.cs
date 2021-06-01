using System;

using GraphData = UnityEditor.ShaderGraph.GraphData;

namespace UnityEditor.ShaderGraph
{
    // An action takes in a reference to a GraphData object and performs some modification on it
    interface IGraphDataAction
    {
        Action<GraphData> modifyGraphDataAction { get; }
    }
}
