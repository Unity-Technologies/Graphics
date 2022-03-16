using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace com.unity.shadergraph.defs
{

    internal class MinimumNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,     // Version
            "Minimum", // Name
            "Out = min(A, B);",
            new ParameterDescriptor("A", TYPE.Vector, GraphType.Usage.In),
            new ParameterDescriptor("B", TYPE.Vector, GraphType.Usage.In, new float[] { 1f, 1f, 1f, 1f }),
            new ParameterDescriptor("Out", TYPE.Vector, GraphType.Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Category", "Math, Range" },
            { "Name.Synonyms", "least, littlest, smallest, lesser" },
            { "Tooltip", "returns the smaller of A and B" },
            { "Parameters.A.Tooltip", "Input A" },
            { "Parameters.B.Tooltip", "Input B" },
            { "Parameters.Out.Tooltip", "the lesser of A or B" }
        };
    }
}
