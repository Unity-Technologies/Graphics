using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class NegateNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,
            "Negate",
            "Out = -1 * In;",
            new ParameterDescriptor("In", TYPE.Vector, GraphType.Usage.In),
            new ParameterDescriptor("Out", TYPE.Vector, GraphType.Usage.Out)
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
