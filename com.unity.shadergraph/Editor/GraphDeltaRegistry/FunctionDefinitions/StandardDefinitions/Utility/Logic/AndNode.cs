using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace com.unity.shadergraph.defs
{

    internal class AndNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,     // Version
            "And", // Name
            "Out = A && B;",
            new ParameterDescriptor("A", TYPE.Bool, Usage.In),
            new ParameterDescriptor("B", TYPE.Bool, Usage.In),
            new ParameterDescriptor("Out", TYPE.Bool, Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Category", "Utility, Logic" },
            { "Name.Synonyms", "&&" },
            { "Tooltip", "returns true if both A and B are true" },
            { "Parameters.A.Tooltip", "Input A" },
            { "Parameters.B.Tooltip", "Input B" },
            { "Parameters.Out.Tooltip", "true if both A and B are true" }
        };
    }
}
