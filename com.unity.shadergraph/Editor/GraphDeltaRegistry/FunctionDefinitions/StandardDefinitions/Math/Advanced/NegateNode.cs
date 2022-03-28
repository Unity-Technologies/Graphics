using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class NegateNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,
            "Negate",
            "Out = -1 * In;",
            new ParameterDescriptor("In", TYPE.Vector, Usage.In),
            new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Name.Synonyms", "invert, opposite, -" },
            { "Tooltip", "multiplies the input by negative 1" },
            { "Parameters.In.Tooltip", "input value" },
            { "Parameters.Out.Tooltip", "the negated version of the input" },
            { "Category", "Math, Advanced" }
        };
    }
}
