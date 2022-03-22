using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace com.unity.shadergraph.defs
{

    internal class LengthNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,
            "Length",
            "Out = length(In);",
            new ParameterDescriptor("In", TYPE.Vector, Usage.In),
            new ParameterDescriptor("Out", TYPE.Float, Usage.Out)
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
