using System.Collections.Generic;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
{

    internal class DotProductNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,     // Version
            "Dot Product", // Name
            "Out = dot(A, B);",
            new ParameterDescriptor("A", TYPE.Any, Usage.In),
            new ParameterDescriptor("B", TYPE.Any, Usage.In),
            new ParameterDescriptor("Out", TYPE.Float, Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Name.Synonyms", "" },
            { "Tooltip", "returns the dot product between two vectors" },
            { "Parameters.A.Tooltip", "Input A" },
            { "Parameters.B.Tooltip", "Input B" },
            { "Parameters.Out.Tooltip", "the dot product of A and B" }
        };
    }
}
