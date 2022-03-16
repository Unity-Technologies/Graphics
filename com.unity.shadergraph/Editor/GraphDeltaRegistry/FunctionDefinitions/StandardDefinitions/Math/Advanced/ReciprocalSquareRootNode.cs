using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace com.unity.shadergraph.defs
{

    internal class ReciprocalSquareRootNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,
            "ReciprocalSquareRoot",
            "Out = rsqrt(In);",
            new ParameterDescriptor("In", TYPE.Vector, GraphType.Usage.In, new float[] {1f, 1f, 1f, 1f}),
            new ParameterDescriptor("Out", TYPE.Vector, GraphType.Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Name.Synonyms", "rsqrt, inversesqrt" },
            { "Tooltip", "divides 1 by the square root of the input" },
            { "Parameters.In.Tooltip", "input value" },
            { "Parameters.Out.Tooltip", "1 divided by the square root of the input" },
            { "Category", "Math, Advanced" },
            { "DisplayName", "Reciprocal Square Root" }
        };
    }
}
