using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class NandNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,     // Version
            "Nand", // Name
            "Out = !A && !B;",
            new ParameterDescriptor("A", TYPE.Bool, Usage.In),
            new ParameterDescriptor("B", TYPE.Bool, Usage.In),
            new ParameterDescriptor("Out", TYPE.Bool, Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Category", "Utility, Logic" },
            { "Tooltip", "returns true if both inputs are false" },
            { "Parameters.A.Tooltip", "Input A" },
            { "Parameters.B.Tooltip", "Input B" },
            { "Parameters.Out.Tooltip", "true if A and B are false" }
        };

        public static Dictionary<string, float> UIHints => new()
        {
            { "Preview.Exists", 0 }
        };
    }
}
