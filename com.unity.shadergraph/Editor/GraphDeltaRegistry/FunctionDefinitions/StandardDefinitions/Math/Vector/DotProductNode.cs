using System.Collections.Generic;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
{

    internal class DotProductNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,     // Version
            "DotProduct", // Name
            "Out = dot(A, B);",
            new ParameterDescriptor("A", TYPE.Vector, Usage.In),
            new ParameterDescriptor("B", TYPE.Vector, Usage.In),
            new ParameterDescriptor("Out", TYPE.Float, Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "DisplayName", "Dot Product" },
            { "Category", "Math, Vector" },
            { "Tooltip", "returns the dot product between two vectors" },
            { "Parameters.Out.Tooltip", "the dot product of A and B" }
        };
    }
}
