using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class LengthNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,
            "Length",
            "Out = length(In);",
            new ParameterDescriptor("In", TYPE.Vector, GraphType.Usage.In),
            new ParameterDescriptor("Out", TYPE.Float, GraphType.Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Name.Synonyms", "measure" },
            { "Tooltip", "returns the magnitude or length of the input vector " },
            { "Parameters.In.Tooltip", "input vector" },
            { "Parameters.Out.Tooltip", "the length of the input vector" },
            { "Category", "Math, Advanced" }
        };
    }
}
