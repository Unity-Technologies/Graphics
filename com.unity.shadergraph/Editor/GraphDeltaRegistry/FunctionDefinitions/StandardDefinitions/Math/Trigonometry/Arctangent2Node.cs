using System.Collections.Generic;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
{

    internal class Arctangent2Node : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,     // Version
            "Arctangent2", // Name
            "Out = atan2(A, B);",
            new ParameterDescriptor("A", TYPE.Any, Usage.In),
            new ParameterDescriptor("B", TYPE.Any, Usage.In),
            new ParameterDescriptor("Out", TYPE.Any, Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Name.Synonyms", "atan2" },
            { "Tooltip", "returns the arctangent of A divided by B" },
            { "Parameters.A.Tooltip", "Input A" },
            { "Parameters.B.Tooltip", "Input B" },
            { "Parameters.Out.Tooltip", "the arctangent of A divided by B" }
        };
    }
}
