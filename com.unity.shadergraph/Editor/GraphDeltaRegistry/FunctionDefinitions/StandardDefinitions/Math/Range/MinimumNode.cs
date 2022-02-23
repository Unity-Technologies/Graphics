using System.Collections.Generic;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
{

    internal class MinimumNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,     // Version
            "Minimum", // Name
            "Out = min(A, B);",
            new ParameterDescriptor("A", TYPE.Vector, Usage.In),
            new ParameterDescriptor("B", TYPE.Vector, Usage.In), //defaults to 1
            new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Category", "Math, Range" },
            { "Name.Synonyms", "least, littlest, smallest, lesser" },
            { "Tooltip", "returns the smaller of A and B" },
            { "Parameters.Out.Tooltip", "the lesser of A or B" }
        };
    }
}
