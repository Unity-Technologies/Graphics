using System.Collections.Generic;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
{

    internal class MaximumNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,     // Version
            "Maximum", // Name
            "Out = max(A, B);",
            new ParameterDescriptor("A", TYPE.Any, Usage.In),
            new ParameterDescriptor("B", TYPE.Any, Usage.In),
            new ParameterDescriptor("Out", TYPE.Any, Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Name.Synonyms", "biggest, greatest, largest, most" },
            { "Tooltip", "returns the greater of A or B" },
            { "Parameters.A.Tooltip", "Input A" },
            { "Parameters.B.Tooltip", "Input B" },
            { "Parameters.Out.Tooltip", "the greater of the two input values" }
        };
    }
}
