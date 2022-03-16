using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class HyperbolicCosineNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,
            "HyperbolicCosine",
            "Out = cosh(In);",
            new ParameterDescriptor("In", TYPE.Vector, GraphType.Usage.In),
            new ParameterDescriptor("Out", TYPE.Vector, GraphType.Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Name.Synonyms", "cosh" },
            { "Tooltip", "returns the hyperbolic cosine of the input" },
            { "Parameters.In.Tooltip", "input value" },
            { "Parameters.Out.Tooltip", "the hyperbolic cosine of the input" },
            { "Category", "Math, Trigonometry" },
            { "DisplayName", "Hyperbolic Cosine" }
        };
    }
}
