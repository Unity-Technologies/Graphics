using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class TriangleWaveNode : IStandardNode
    {
        public static string Name = "TriangleWave";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
            "Out = 2.0 * abs( 2 * (In - floor(0.5 + In)) ) - 1.0;",
            new ParameterDescriptor("In", TYPE.Vector, Usage.In),
            new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Triangle Wave",
            tooltip: "creates a linear wave form with triangular shapes",
            categories: new string[2] { "Math", "Wave" },
            synonyms: new string[1] { "sawtooth wave" },
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "In",
                    tooltip: "input value"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "a triangle wave"
                )
            }
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Name.Synonyms", "sawtooth wave" },
            { "Tooltip", "creates a linear wave form with triangular shapes" },
            { "Category", "Math, Wave" },
            { "DisplayName", "Triangle Wave" },
            { "Parameters.In.Tooltip", "input value" },
            { "Parameters.Out.Tooltip", "a triangle wave" }
        };
    }
}
