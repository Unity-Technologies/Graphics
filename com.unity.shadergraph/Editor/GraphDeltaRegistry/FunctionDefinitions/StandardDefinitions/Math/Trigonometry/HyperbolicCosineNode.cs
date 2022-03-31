using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class HyperbolicCosineNode : IStandardNode
    {
        public static string Name = "HyperbolicCosine";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
            "Out = cosh(In);",
            new ParameterDescriptor("In", TYPE.Vector, Usage.In),
            new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Hyperbolic Cosine",
            tooltip: "returns the hyperbolic cosine of the input",
            categories: new string[2] { "Math", "Trigonometry" },
            synonyms: new string[1] { "cosh" },
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "In",
                    tooltip: "input value"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "the hyperbolic cosine of the input"
                )
            }
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Name.Synonyms", "cosh" },
            { "Tooltip", "returns the hyperbolic cosine of the input" },
            { "Category", "Math, Trigonometry" },
            { "DisplayName", "Hyperbolic Cosine" },
            { "Parameters.In.Tooltip", "input value" },
            { "Parameters.Out.Tooltip", "the hyperbolic cosine of the input" },
        };
    }
}
