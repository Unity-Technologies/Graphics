using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class SineNode : IStandardNode
    {
        public static string Name = "Sine";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
            "Out = sin(In);",
            new ParameterDescriptor("In", TYPE.Vector, Usage.In),
            new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "returns the sine of the input",
            categories: new string[2] { "Math", "Trigonometry" },
            synonyms: new string[0],
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "In",
                    tooltip: "input value"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "the sine of the input"
                )
            }
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Tooltip", "returns the sine of the input" },
            { "Category", "Math, Trigonometry" },
            { "Parameters.In.Tooltip", "input value" },
            { "Parameters.Out.Tooltip", "the sine of the input" }
        };
    }
}
