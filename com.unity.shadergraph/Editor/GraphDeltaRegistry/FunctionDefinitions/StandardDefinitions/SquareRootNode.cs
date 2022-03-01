using System.Collections.Generic;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
{

    internal class SquareRootNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,
            "SquareRoot",
            "Out = sqrt(In);",
            new ParameterDescriptor("In", TYPE.Vector, Usage.In),
            new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
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
