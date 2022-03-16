using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class CosineNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,
            "Cosine",
            "Out = cos(In);",
            new ParameterDescriptor("In", TYPE.Vector, GraphType.Usage.In),
            new ParameterDescriptor("Out", TYPE.Vector, GraphType.Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Tooltip", "returns the cosine of the input" },
            { "Parameters.In.Tooltip", "input value" },
            { "Parameters.Out.Tooltip", "the cosine of the input" },
            { "Category", "Math, Trigonometry" }
        };
    }
}
