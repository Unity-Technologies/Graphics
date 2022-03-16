using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class HyperbolicTangentNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,
            "HyperbolicTangent",
            "Out = tanh(In);",
            new ParameterDescriptor("In", TYPE.Vector, GraphType.Usage.In),
            new ParameterDescriptor("Out", TYPE.Vector, GraphType.Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Name.Synonyms", "tanh" },
            { "Tooltip", "returns the hyperbolic tangent of the input" },
            { "Parameters.In.Tooltip", "input value" },
            { "Parameters.Out.Tooltip", "the hyperbolic tangent of the input" },
            { "Category", "Math, Trigonometry" },
            { "DisplayName", "Hyperbolic Tangent" }
        };
    }
}
