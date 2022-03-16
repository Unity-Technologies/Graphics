using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace com.unity.shadergraph.defs
{

    internal class DistanceNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,     // Version
            "Distance", // Name
            "Out = distance(A, B);",
            new ParameterDescriptor("A", TYPE.Vector, GraphType.Usage.In),
            new ParameterDescriptor("B", TYPE.Vector, GraphType.Usage.In),
            new ParameterDescriptor("Out", TYPE.Float, GraphType.Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Category", "Math, Vector" },
            { "Name.Synonyms", "length" },
            { "Tooltip", "returns the distance between A and B" },
            { "Parameters.A.Tooltip", "Point A" },
            { "Parameters.B.Tooltip", "Point B" },
            { "Parameters.Out.Tooltip", "the distance between A and B" }
        };
    }
}
