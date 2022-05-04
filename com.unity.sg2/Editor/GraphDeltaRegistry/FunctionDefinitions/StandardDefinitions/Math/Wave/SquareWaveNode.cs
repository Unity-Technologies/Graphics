using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class SquareWaveNode : IStandardNode
    {
        public static string Name = "SquareWave";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
            "Out = 1.0 - 2.0 * round(frac(In));",
            new ParameterDescriptor("In", TYPE.Vector, Usage.In),
            new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Square Wave",
            tooltip: "Creates a wave where the amplitude alternates between fixed min and max values at a steady frequency.",
            categories: new string[2] { "Math", "Wave" },
            synonyms: new string[1] { "triangle wave" },
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "In",
                    tooltip: "input value"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "a square wave"
                )
            }
        );
    }
}
