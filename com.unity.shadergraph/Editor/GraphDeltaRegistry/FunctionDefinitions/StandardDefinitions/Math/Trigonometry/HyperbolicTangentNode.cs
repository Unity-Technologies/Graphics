using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class HyperbolicTangentNode : IStandardNode
    {
        public static string Name = "HyperbolicTangent";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
            "Out = tanh(In);",
            new ParameterDescriptor("In", TYPE.Vector, Usage.In),
            new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Hyperbolic Tangent",
            tooltip: "returns the hyperbolic tangent of the input",
            categories: new string[2] { "Math", "Trigonometry" },
            synonyms: new string[1] { "tanh" },
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "In",
                    tooltip: "input value"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "the hyperbolic tangent of the input"
                )
            }
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Name.Synonyms", "tanh" },
            { "Tooltip", "returns the hyperbolic tangent of the input" },
            { "Category", "Math, Trigonometry" },
            { "DisplayName", "Hyperbolic Tangent" },
            { "Parameters.In.Tooltip", "input value" },
            { "Parameters.Out.Tooltip", "the hyperbolic tangent of the input" }
        };
    }
}
