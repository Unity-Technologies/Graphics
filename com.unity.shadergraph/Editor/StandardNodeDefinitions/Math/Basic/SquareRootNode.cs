using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class SquareRootNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,
            "SquareRoot",
            "Out = sqrt(In);",
            new ParameterDescriptor("In", TYPE.Vector, GraphType.Usage.In),
            new ParameterDescriptor("Out", TYPE.Vector, GraphType.Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Name.Synonyms", "sqrt" },
            { "Tooltip", "returns the square root of the input" },
            { "Parameters.In.Tooltip", "input value" },
            { "Parameters.Out.Tooltip", "the square root of the input" },
            { "Category", "Math, Basic" },
            { "DisplayName", "Square Root" }
        };
    }
}
