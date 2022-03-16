using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class Arctangent2Node : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,     // Version
            "Arctangent2", // Name
            "Out = atan2(A, B);",
            new ParameterDescriptor("A", TYPE.Vector, GraphType.Usage.In),
            new ParameterDescriptor("B", TYPE.Vector, GraphType.Usage.In),
            new ParameterDescriptor("Out", TYPE.Vector, GraphType.Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Category", "Math, Trigonometry" },
            { "Name.Synonyms", "atan2" },
            { "Tooltip", "returns the arctangent of A divided by B" },
            { "Parameters.A.Tooltip", "the numerator" },
            { "Parameters.B.Tooltip", "the denominator" },
            { "Parameters.Out.Tooltip", "the arctangent of A divided by B" }
        };
    }
}
